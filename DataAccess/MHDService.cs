using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using mhd.Domain;

namespace mhd.DataAccess;

public class MHDService : IMHDService
{
    private const string PersonnelCacheKey = "mhd:personnel";
    private const string AircraftCacheKey = "mhd:aircraft";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    private readonly IDbContextFactory<DatabaseContext> factory;
    private readonly IMemoryCache cache;

    public MHDService(IDbContextFactory<DatabaseContext> factory, IMemoryCache cache)
    {
        this.factory = factory;
        this.cache = cache;
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

        return bio!;
    }

    public async Task<List<BioSummary>> QueryDocumentAsync()
    {
        EnsureCosmosConfigured();
        using var context = factory.CreateDbContext();
        var documents = await context.Bio.ToListAsync();
        return documents.Select(d => new BioSummary(d)).OrderBy(ds => ds.PerIdentification).ToList();
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

        var bioIdsTask = bioContext.Bio
            .Where(b => b.perIdentification != null)
            .Select(b => b.perIdentification)
            .ToListAsync(cancellationToken);

        HashSet<string>? bioIds = null;
        var flushed = 0;

        await foreach (var d in personnelContext.Personnel.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (bioIds == null && bioIdsTask.IsCompletedSuccessfully)
            {
                bioIds = (await bioIdsTask).ToHashSet();
            }

            if (d.DeceasedDate == "12/30/1899")
            {
                d.DeceasedDate = "";
            }

            destination.Add(new PersonnelSummary(d, bioIds != null && bioIds.Contains(d.perIdentification)));

            if (ShouldFlush(destination.Count, flushed))
            {
                flushed = destination.Count;
                progress?.Report(flushed);
            }
        }

        bioIds ??= (await bioIdsTask).ToHashSet();
        foreach (var person in destination)
        {
            person.HasBio = !string.IsNullOrEmpty(person.PerIdentification) && bioIds.Contains(person.PerIdentification);
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

        using var context = factory.CreateDbContext();
        var flushed = 0;

        await foreach (var aircraft in context.Aircraft.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            if (aircraft.acFinalAircraftDisposition == "Aircraft Final Disposition")
            {
                aircraft.acFinalAircraftDisposition = "";
            }

            destination.Add(aircraft);

            if (ShouldFlush(destination.Count, flushed))
            {
                flushed = destination.Count;
                progress?.Report(flushed);
            }
        }

        destination.Sort((a, b) => string.Compare(a.acAircraftNo, b.acAircraftNo, StringComparison.OrdinalIgnoreCase));
        cache.Set(AircraftCacheKey, destination.ToList(), CacheDuration);
        progress?.Report(destination.Count);
    }

    private static bool ShouldFlush(int count, int flushed) =>
        count == 40 || (count - flushed) >= 200;

    public async Task<Aircraft> LoadAircraftMissionCrewSummaryAsync(string aircraftNo)
    {
        EnsureCosmosConfigured();
        var key = $"mhd:missions:{aircraftNo}";
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
                mission.MissionCrew = crewByMission[mission.misMissionNo].ToList();
            }

            var result = new Aircraft
            {
                acAircraftNo = aircraftNo,
                Mission = missions.OrderBy(m => m.misMissionNo).ToList()
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
}
