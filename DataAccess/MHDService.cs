using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using mhd.Domain;

namespace mhd.DataAccess;

public class MHDService : IMHDService
{
    private const string PersonnelCacheKey = "mhd:personnel";
    private const string AircraftCacheKey = "mhd:aircraft";
    private const string PictureIndexCacheKey = "mhd:pictures";
    private const string DefaultPicsBase = "https://mhd09192023.blob.core.windows.net/pics";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    private readonly IDbContextFactory<DatabaseContext> factory;
    private readonly IMemoryCache cache;
    private readonly IWebHostEnvironment env;

    public MHDService(IDbContextFactory<DatabaseContext> factory, IMemoryCache cache, IWebHostEnvironment env)
    {
        this.factory = factory;
        this.cache = cache;
        this.env = env;
    }

    private static void EnsureCosmosConfigured()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COSMOS_ENDPOINT", EnvironmentVariableTarget.Process)) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("COSMOS_DATABASE", EnvironmentVariableTarget.Process)))
        {
            throw new InvalidOperationException("Cosmos DB is not configured.");
        }
    }

    public async Task<Bio> LoadBioAsync(string perIdentification)
    {
        EnsureCosmosConfigured();
        var key = $"mhd:bio:{perIdentification}";
        var bio = await cache.GetOrCreateAsync(key, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            using var context = factory.CreateDbContext();
            return await context.Bio
                .WithPartitionKey(perIdentification)
                .SingleOrDefaultAsync(d => d.perIdentification == perIdentification);
        });

        return Bio.HasMeaningfulContent(bio) ? bio! : null!;
    }

    public async Task<List<BioSummary>> QueryDocumentAsync()
    {
        EnsureCosmosConfigured();
        using var context = factory.CreateDbContext();
        var documents = await context.Bio.ToListAsync();
        return documents
            .Where(Bio.HasMeaningfulContent)
            .Select(d => new BioSummary(d))
            .OrderBy(ds => ds.PerIdentification)
            .ToList();
    }

    public async Task<List<PersonnelSummary>> QueryPersonnelAsync()
    {
        var list = new List<PersonnelSummary>();
        await FillPersonnelAsync(list);
        return list;
    }

    public async Task FillPersonnelAsync(List<PersonnelSummary> destination, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        EnsureCosmosConfigured();
        if (cache.TryGetValue(PersonnelCacheKey, out List<PersonnelSummary>? cached) && cached != null)
        {
            destination.AddRange(cached);
            progress?.Report(destination.Count);
            return;
        }

        using var bioContext = factory.CreateDbContext();
        using var personnelContext = factory.CreateDbContext();
        using var crewContext = factory.CreateDbContext();
        using var missionContext = factory.CreateDbContext();

        var biosTask = bioContext.Bio.ToListAsync(cancellationToken);
        var personnelTask = personnelContext.Personnel.ToListAsync(cancellationToken);
        var crewTask = crewContext.MissionCrew.ToListAsync(cancellationToken);
        var missionTask = missionContext.Mission.ToListAsync(cancellationToken);
        await Task.WhenAll(biosTask, personnelTask, crewTask, missionTask);

        var bioIds = biosTask.Result
            .Where(Bio.HasMeaningfulContent)
            .Select(b => b.perIdentification)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet();
        var sortieKeys = missionTask.Result
            .Where(m => !string.IsNullOrWhiteSpace(m.acAircraftNo) && !string.IsNullOrWhiteSpace(m.misMissionNo))
            .Select(m => $"{m.acAircraftNo}\t{m.misMissionNo}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var kiaLink = new Dictionary<string, (string Aircraft, string Mission, bool Confirmed)>(StringComparer.OrdinalIgnoreCase);
        foreach (var crew in crewTask.Result)
        {
            if (!CrewStatus.IsKia(crew.Status) ||
                string.IsNullOrWhiteSpace(crew.perIdentification) ||
                string.IsNullOrWhiteSpace(crew.acAircraftNo) ||
                string.IsNullOrWhiteSpace(crew.misMissionNo))
            {
                continue;
            }

            var confirmed = sortieKeys.Contains($"{crew.acAircraftNo}\t{crew.misMissionNo}");
            if (!kiaLink.TryGetValue(crew.perIdentification, out var existing) || (!existing.Confirmed && confirmed))
            {
                kiaLink[crew.perIdentification] = (crew.acAircraftNo, crew.misMissionNo, confirmed);
            }
        }

        foreach (var d in personnelTask.Result)
        {
            if (d.DeceasedDate == "12/30/1899")
            {
                d.DeceasedDate = "";
            }

            kiaLink.TryGetValue(d.perIdentification, out var link);
            var hasKia = !string.IsNullOrEmpty(link.Aircraft) || CrewStatus.IsKiaFlag(d.perKIA);
            destination.Add(new PersonnelSummary(
                d,
                bioIds.Contains(d.perIdentification),
                hasKia,
                link.Aircraft,
                link.Mission,
                HasPictures(d.perIdentification)));
            if (destination.Count == 40 || destination.Count % 500 == 0)
            {
                progress?.Report(destination.Count);
            }
        }

        destination.Sort((a, b) =>
        {
            var last = string.Compare(a.LastName, b.LastName, StringComparison.OrdinalIgnoreCase);
            return last != 0 ? last : string.Compare(a.FirstName, b.FirstName, StringComparison.OrdinalIgnoreCase);
        });

        cache.Set(PersonnelCacheKey, destination.ToList(), CacheDuration);
        progress?.Report(destination.Count);
    }

    public async Task<List<Aircraft>> QueryAircraftAsync()
    {
        var list = new List<Aircraft>();
        await FillAircraftAsync(list);
        return list;
    }

    public async Task FillAircraftAsync(List<Aircraft> destination, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        EnsureCosmosConfigured();
        if (cache.TryGetValue(AircraftCacheKey, out List<Aircraft>? cached) && cached != null)
        {
            destination.AddRange(cached);
            progress?.Report(destination.Count);
            return;
        }

        using var aircraftContext = factory.CreateDbContext();
        using var missionContext = factory.CreateDbContext();
        var aircraftTask = aircraftContext.Aircraft.ToListAsync(cancellationToken);
        var missionAircraftTask = missionContext.Mission
            .Select(m => m.acAircraftNo)
            .ToListAsync(cancellationToken);
        await Task.WhenAll(aircraftTask, missionAircraftTask);

        var aircraftWithMissions = missionAircraftTask.Result
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var aircraft in aircraftTask.Result)
        {
            if (aircraft.acFinalAircraftDisposition == "Aircraft Final Disposition")
            {
                aircraft.acFinalAircraftDisposition = "";
            }

            aircraft.HasMissions = !string.IsNullOrWhiteSpace(aircraft.acAircraftNo)
                && aircraftWithMissions.Contains(aircraft.acAircraftNo);

            destination.Add(aircraft);
            if (destination.Count == 40 || destination.Count % 200 == 0)
            {
                progress?.Report(destination.Count);
            }
        }

        destination.Sort((a, b) => string.Compare(a.acAircraftNo, b.acAircraftNo, StringComparison.OrdinalIgnoreCase));
        cache.Set(AircraftCacheKey, destination.ToList(), CacheDuration);
        progress?.Report(destination.Count);
    }

    public async Task<Aircraft> LoadAircraftMissionCrewSummaryAsync(string aircraftNo, string? acBG = null)
    {
        EnsureCosmosConfigured();
        var key = $"mhd:missions:{aircraftNo}:{acBG}";
        if (cache.TryGetValue(key, out Aircraft? cached) && cached != null)
        {
            return cached;
        }

        using var context = factory.CreateDbContext();

        try
        {
            var missions = await context.Mission
                .Where(m => m.acAircraftNo == aircraftNo)
                .ToListAsync();

            var crew = missions.Count == 0
                ? new List<MissionCrew>()
                : await context.MissionCrew
                    .Where(mc => mc.acAircraftNo == aircraftNo)
                    .ToListAsync();

            var crewByMission = crew.ToLookup(mc => mc.misMissionNo);
            foreach (var mission in missions)
            {
                mission.MissionCrew = crewByMission[mission.misMissionNo].ToList() ?? new List<MissionCrew>();
            }

            var bg = acBG;
            if (string.IsNullOrWhiteSpace(bg))
            {
                bg = missions.Select(m => m.acmBG).FirstOrDefault(g => !string.IsNullOrWhiteSpace(g));
            }

            if (!string.IsNullOrWhiteSpace(bg) && missions.Count > 0)
            {
                try
                {
                    var targets = await context.MissionTarget
                        .WithPartitionKey(bg)
                        .ToListAsync();
                    var byNo = targets
                        .Where(t => !string.IsNullOrWhiteSpace(t.misMissionNo))
                        .GroupBy(t => t.misMissionNo)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
                    foreach (var mission in missions)
                    {
                        if (mission.misMissionNo != null && byNo.TryGetValue(mission.misMissionNo, out var target))
                        {
                            mission.TargetCity = target.City ?? string.Empty;
                            mission.TargetCountry = target.Country ?? string.Empty;
                            mission.Target = target.Target ?? string.Empty;
                            mission.MissionDate = target.MissionDate ?? string.Empty;
                        }
                    }
                }
                catch (CosmosException)
                {
                }
            }

            var result = new Aircraft
            {
                acAircraftNo = aircraftNo,
                acBG = bg ?? string.Empty,
                Mission = missions
                    .OrderBy(m => int.TryParse(m.misMissionNo, out var n) ? n : int.MaxValue)
                    .ToList()
            };
            cache.Set(key, result, CacheDuration);
            return result;
        }
        catch (CosmosException)
        {
            return new Aircraft
            {
                acAircraftNo = aircraftNo,
                Mission = new List<Mission>()
            };
        }
    }

    public void InvalidateListCache()
    {
        cache.Remove(PersonnelCacheKey);
        cache.Remove(AircraftCacheKey);
    }

    public bool HasPictures(string perIdentification) =>
        !string.IsNullOrWhiteSpace(perIdentification) && GetPictureIndex().ContainsKey(perIdentification.Trim());

    public IReadOnlyList<PersonPicture> GetPersonPictures(string perIdentification)
    {
        if (string.IsNullOrWhiteSpace(perIdentification))
        {
            return Array.Empty<PersonPicture>();
        }

        return GetPictureIndex().TryGetValue(perIdentification.Trim(), out var pictures)
            ? pictures
            : Array.Empty<PersonPicture>();
    }

    private Dictionary<string, IReadOnlyList<PersonPicture>> GetPictureIndex()
    {
        if (cache.TryGetValue(PictureIndexCacheKey, out Dictionary<string, IReadOnlyList<PersonPicture>>? cached) && cached != null)
        {
            return cached;
        }

        var map = new Dictionary<string, IReadOnlyList<PersonPicture>>(StringComparer.OrdinalIgnoreCase);
        var path = Path.Combine(env.WebRootPath ?? "wwwroot", "data", "pictures.json");
        if (File.Exists(path))
        {
            try
            {
                using var stream = File.OpenRead(path);
                var raw = JsonSerializer.Deserialize<Dictionary<string, List<PictureRecord>>>(stream, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                var picsBase = (Environment.GetEnvironmentVariable("PICS_BASE") ?? DefaultPicsBase).TrimEnd('/');
                if (raw != null)
                {
                    foreach (var pair in raw)
                    {
                        var pictures = (pair.Value ?? new List<PictureRecord>())
                            .Where(p => !string.IsNullOrWhiteSpace(p.Stem))
                            .Select(p => new PersonPicture
                            {
                                Kind = p.Kind ?? string.Empty,
                                Stem = p.Stem!,
                                Width = p.Width,
                                Height = p.Height,
                                BmpUrl = $"{picsBase}/bmp/{p.Stem}.bmp",
                                EnhancedUrl = $"{picsBase}/4k/{p.Stem}.jpg"
                            })
                            .ToList();
                        if (pictures.Count > 0)
                        {
                            map[pair.Key] = pictures;
                        }
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        cache.Set(PictureIndexCacheKey, map, TimeSpan.FromHours(6));
        return map;
    }

    private sealed class PictureRecord
    {
        public string? Kind { get; set; }
        public string? Stem { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
