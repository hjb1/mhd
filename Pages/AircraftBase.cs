using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using mhd.Domain;

namespace mhd.Pages
{
    public class AircraftBase : ComponentBase, IDisposable
    {
        protected const int VisibleCap = 300;

        [Inject]
        protected IMHDService MHDService { get; set; } = default!;
        [Inject]
        protected ILogger<AircraftBase> Logger { get; set; } = default!;
        [Inject]
        protected NavigationManager Nav { get; set; } = default!;
        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        protected string? OpenAircraftNo { get; set; }
        protected string? OpenMissionNo { get; set; }
        protected string? HighlightCrewId { get; set; }
        protected int FindEpoch { get; set; }

        protected List<mhd.Domain.Aircraft> aircraftList { get; set; } = new();
        protected List<mhd.Domain.Aircraft> View { get; set; } = new();
        protected bool ShowMissionsDialog { get; set; }
        protected mhd.Domain.Aircraft SelectedAirCraft { get; set; } = new();
        protected mhd.Domain.Aircraft missionCrewSummaries { get; set; } = new()
        {
            Mission = new List<Mission>()
        };
        protected Dictionary<string, string> PersonnelNames { get; set; } = new();
        protected string? ExpandedMissionNo { get; set; }
        protected string Filter { get; set; } = string.Empty;
        protected bool Only44th { get; set; }
        protected string SortColumn { get; set; } = "acAircraftNo";
        protected bool SortAscending { get; set; } = true;
        protected string? LoadError { get; set; }
        protected bool IsScanning { get; set; } = true;
        protected bool ShowFullList { get; set; }
        protected bool MissionsLoading { get; set; }

        private CancellationTokenSource? debounceCts;
        private readonly CancellationTokenSource lifetimeCts = new();
        private bool disposed;
        private string? appliedOpen;

        protected IEnumerable<mhd.Domain.Aircraft> VisibleRows => View.Take(VisibleCap);

        protected override async Task OnInitializedAsync()
        {
            Nav.LocationChanged += OnLocationChanged;
            await ReloadAsync(invalidate: false);
        }

        protected override async Task OnParametersSetAsync()
        {
            await ApplyIncomingOpenAsync();
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            _ = InvokeAsync(async () =>
            {
                await ApplyIncomingOpenAsync();
                StateHasChanged();
            });
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || disposed || ShowMissionsDialog)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(QueryString.Get(Nav, "ac") ?? OpenAircraftNo))
            {
                await ApplyIncomingOpenAsync();
                await SafeStateHasChangedAsync();
            }
        }

        protected async Task ReloadAsync(bool invalidate)
        {
            if (disposed)
            {
                return;
            }

            IsScanning = true;
            LoadError = null;
            try
            {
                if (invalidate)
                {
                    MHDService.InvalidateListCache();
                }

                ShowFullList = false;
                aircraftList = await MHDService.QueryAircraftAsync();
                if (disposed || lifetimeCts.IsCancellationRequested)
                {
                    return;
                }

                ApplyView();
                ShowFullList = true;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (disposed)
                {
                    return;
                }

                Logger.LogError(ex, "Aircraft load failed");
                LoadError = $"Could not open Aircraft. {ex.GetType().Name}: {ex.Message}";
                aircraftList = new List<mhd.Domain.Aircraft>();
                View = new List<mhd.Domain.Aircraft>();
            }
            finally
            {
                if (!disposed)
                {
                    IsScanning = false;
                    await ApplyIncomingOpenAsync();
                }
            }
        }

        protected void OnFilterInput(ChangeEventArgs e)
        {
            Filter = e.Value?.ToString() ?? string.Empty;
            debounceCts?.Cancel();
            debounceCts = new CancellationTokenSource();
            var token = debounceCts.Token;
            _ = DebounceApplyAsync(token);
        }

        private async Task DebounceApplyAsync(CancellationToken token)
        {
            try
            {
                await Task.Delay(160, token);
                if (disposed || token.IsCancellationRequested)
                {
                    return;
                }

                await InvokeAsync(() =>
                {
                    if (disposed)
                    {
                        return;
                    }

                    ApplyView();
                    StateHasChanged();
                });
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (JSDisconnectedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        protected void OnOnly44thChanged(ChangeEventArgs e)
        {
            Only44th = e.Value is bool flag
                ? flag
                : string.Equals(e.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
            ApplyView();
        }

        protected void SortBy(string column)
        {
            if (SortColumn == column)
            {
                SortAscending = !SortAscending;
            }
            else
            {
                SortColumn = column;
                SortAscending = true;
            }

            ApplyView();
            StateHasChanged();
        }

        protected string SortMark(string column)
        {
            if (SortColumn != column)
            {
                return string.Empty;
            }

            return SortAscending ? " ▲" : " ▼";
        }

        protected void SelectRow(mhd.Domain.Aircraft aircraft)
        {
            SelectedAirCraft = aircraft;
        }

        protected bool IsHighlightedCrew(MissionCrew crew) =>
            !string.IsNullOrWhiteSpace(HighlightCrewId) &&
            string.Equals(crew.perIdentification, HighlightCrewId, StringComparison.OrdinalIgnoreCase);

        private async Task ApplyIncomingOpenAsync()
        {
            OpenAircraftNo = QueryString.Get(Nav, "ac") ?? OpenAircraftNo;
            OpenMissionNo = QueryString.Get(Nav, "mission") ?? OpenMissionNo;
            HighlightCrewId = QueryString.Get(Nav, "crew") ?? HighlightCrewId;
            if (disposed || aircraftList.Count == 0 || string.IsNullOrWhiteSpace(OpenAircraftNo))
            {
                return;
            }

            var token = $"{OpenAircraftNo}|{OpenMissionNo}|{HighlightCrewId}";
            if (appliedOpen == token)
            {
                return;
            }

            var wanted = OpenAircraftNo.Trim();
            var aircraft = aircraftList.FirstOrDefault(a => SameId(a.acAircraftNo, wanted))
                ?? aircraftList.FirstOrDefault(a => SameAircraftNo(a.acAircraftNo, wanted));
            Only44th = false;
            if (aircraft == null)
            {
                Filter = wanted;
                FindEpoch++;
                ApplyView();
                Logger.LogWarning("Deep-link aircraft {Aircraft} was not in the list", wanted);
                return;
            }

            Filter = aircraft.acAircraftNo;
            SelectedAirCraft = aircraft;
            appliedOpen = token;
            FindEpoch++;
            ApplyView();
            Logger.LogInformation(
                "Opening missions from query ac={Aircraft} mission={Mission} crew={Crew}",
                aircraft.acAircraftNo, OpenMissionNo, HighlightCrewId);
            await OpenMissionsSafeAsync(aircraft);
        }

        private static bool SameId(string? a, string? b) =>
            string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);

        private static bool SameAircraftNo(string? a, string? b) =>
            string.Equals(NormalizeAircraftNo(a), NormalizeAircraftNo(b), StringComparison.OrdinalIgnoreCase);

        private static string NormalizeAircraftNo(string? value) =>
            (value ?? string.Empty).Trim().Replace("-", "", StringComparison.Ordinal);

        protected Task OpenMissionsByNo(string aircraftNo)
        {
            var aircraft = aircraftList.FirstOrDefault(a => a.acAircraftNo == aircraftNo);
            return aircraft == null ? Task.CompletedTask : OpenMissionsSafeAsync(aircraft);
        }

        protected Task OpenSelectedAsync()
        {
            if (SelectedAirCraft != null && SelectedAirCraft.HasMissions)
            {
                return OpenMissionsSafeAsync(SelectedAirCraft);
            }

            return Task.CompletedTask;
        }

        protected static string PersonnelHref(string perIdentification) =>
            $"personnel?select={Uri.EscapeDataString(perIdentification)}";

        protected static string MissionDateLabel(Mission mission)
        {
            if (string.IsNullOrWhiteSpace(mission.MissionDate))
            {
                return string.Empty;
            }

            return DateTime.TryParse(mission.MissionDate, out var date)
                ? date.ToString("yyyy-MM-dd")
                : mission.MissionDate;
        }

        protected static string MissionTargetLabel(Mission mission)
        {
            var place = string.Join(", ", new[] { mission.TargetCity, mission.TargetCountry }.Where(s => !string.IsNullOrWhiteSpace(s)));
            var target = TrimTargetJunk(mission.Target);
            if (string.IsNullOrWhiteSpace(target))
            {
                return place;
            }

            return string.IsNullOrWhiteSpace(place) ? target : $"{place} — {target}";
        }

        private static string TrimTargetJunk(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim().TrimEnd('(', ')', '#', '%', '*', '+', '-', '=', '$', '!', ' ');
        }

        protected async Task OpenMissionsSafeAsync(mhd.Domain.Aircraft? aircraft)
        {
            try
            {
                await ShowModal(aircraft);
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (JSDisconnectedException)
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "View Missions failed for {Aircraft}", aircraft?.acAircraftNo);
                if (!disposed)
                {
                    MissionsLoading = false;
                    ShowMissionsDialog = true;
                    await SafeStateHasChangedAsync();
                }
            }
        }

        protected async Task ShowModal(mhd.Domain.Aircraft? selectedAirCraftRow)
        {
            if (selectedAirCraftRow == null || disposed)
            {
                return;
            }

            SelectedAirCraft = selectedAirCraftRow;
            ExpandedMissionNo = string.IsNullOrWhiteSpace(OpenMissionNo) ? null : OpenMissionNo.Trim();
            missionCrewSummaries = new mhd.Domain.Aircraft
            {
                acAircraftNo = selectedAirCraftRow.acAircraftNo,
                Mission = new List<Mission>()
            };
            MissionsLoading = true;
            ShowMissionsDialog = true;
            await SafeStateHasChangedAsync();

            try
            {
                var missions = await MHDService.LoadAircraftMissionCrewSummaryAsync(
                    selectedAirCraftRow.acAircraftNo,
                    selectedAirCraftRow.acBG);
                if (disposed)
                {
                    return;
                }

                missionCrewSummaries = missions ?? new mhd.Domain.Aircraft
                {
                    acAircraftNo = selectedAirCraftRow.acAircraftNo,
                    Mission = new List<Mission>()
                };
                missionCrewSummaries.Mission ??= new List<Mission>();
                foreach (var mission in missionCrewSummaries.Mission)
                {
                    mission.MissionCrew ??= new List<MissionCrew>();
                }

                if (!string.IsNullOrWhiteSpace(OpenMissionNo))
                {
                    var match = missionCrewSummaries.Mission.FirstOrDefault(m => SameId(m.misMissionNo, OpenMissionNo));
                    ExpandedMissionNo = match?.misMissionNo ?? OpenMissionNo.Trim();
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Mission load failed for {Aircraft}", selectedAirCraftRow.acAircraftNo);
                if (disposed)
                {
                    return;
                }

                missionCrewSummaries = new mhd.Domain.Aircraft
                {
                    acAircraftNo = selectedAirCraftRow.acAircraftNo,
                    Mission = new List<Mission>()
                };
            }
            finally
            {
                if (!disposed)
                {
                    MissionsLoading = false;
                    await SafeStateHasChangedAsync();
                }
            }

            if (disposed)
            {
                return;
            }

            try
            {
                var personnel = await MHDService.QueryPersonnelAsync();
                if (disposed)
                {
                    return;
                }

                PersonnelNames = personnel
                    .Where(p => !string.IsNullOrEmpty(p.PerIdentification))
                    .GroupBy(p => p.PerIdentification)
                    .ToDictionary(g => g.Key, g => FormatName(g.First()));
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Personnel name lookup failed for missions");
            }

            await ScrollHighlightedCrewAsync();
        }

        private async Task ScrollHighlightedCrewAsync()
        {
            if (disposed || string.IsNullOrWhiteSpace(HighlightCrewId))
            {
                return;
            }

            try
            {
                await JSRuntime.InvokeAsync<int>("mhd.scrollCrew", HighlightCrewId);
            }
            catch (JSDisconnectedException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        protected void ToggleMission(string missionNo)
        {
            ExpandedMissionNo = ExpandedMissionNo == missionNo ? null : missionNo;
        }

        protected string CrewName(MissionCrew crew)
        {
            if (!string.IsNullOrEmpty(crew.perIdentification) &&
                PersonnelNames.TryGetValue(crew.perIdentification, out var name))
            {
                return name;
            }

            return crew.perIdentification ?? string.Empty;
        }

        protected void HideModal()
        {
            ShowMissionsDialog = false;
        }

        private async Task SafeStateHasChangedAsync()
        {
            if (disposed)
            {
                return;
            }

            try
            {
                await InvokeAsync(StateHasChanged);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (JSDisconnectedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void ApplyView()
        {
            IEnumerable<mhd.Domain.Aircraft> query = aircraftList;

            if (Only44th)
            {
                query = query.Where(a => a.acBG == "44th");
            }

            if (!string.IsNullOrWhiteSpace(Filter))
            {
                var term = Filter.Trim();
                query = query.Where(a =>
                    Contains(a.acAircraftNo, term) ||
                    Contains(a.acAircraftLetter, term) ||
                    Contains(a.acAircraftName, term) ||
                    Contains(a.acBG, term) ||
                    Contains(a.acSquadron, term) ||
                    Contains(a.acFinalAircraftDisposition, term));
            }

            query = (SortColumn, SortAscending) switch
            {
                ("acAircraftLetter", true) => query.OrderBy(a => a.acAircraftLetter),
                ("acAircraftLetter", false) => query.OrderByDescending(a => a.acAircraftLetter),
                ("acAircraftName", true) => query.OrderBy(a => a.acAircraftName),
                ("acAircraftName", false) => query.OrderByDescending(a => a.acAircraftName),
                ("acBG", true) => query.OrderBy(a => a.acBG),
                ("acBG", false) => query.OrderByDescending(a => a.acBG),
                ("acSquadron", true) => query.OrderBy(a => a.acSquadron),
                ("acSquadron", false) => query.OrderByDescending(a => a.acSquadron),
                ("acFinalAircraftDisposition", true) => query.OrderBy(a => a.acFinalAircraftDisposition),
                ("acFinalAircraftDisposition", false) => query.OrderByDescending(a => a.acFinalAircraftDisposition),
                ("acAircraftNo", false) => query.OrderByDescending(a => a.acAircraftNo),
                _ => query.OrderBy(a => a.acAircraftNo)
            };

            View = query.ToList();
        }

        private static string FormatName(PersonnelSummary person)
        {
            var last = person.LastName ?? string.Empty;
            var first = person.FirstName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(last) && string.IsNullOrWhiteSpace(first))
            {
                return person.PerIdentification;
            }

            return $"{last}, {first}".Trim(' ', ',');
        }

        private static bool Contains(string? value, string term) =>
            !string.IsNullOrEmpty(value) && value.Contains(term, StringComparison.OrdinalIgnoreCase);

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Nav.LocationChanged -= OnLocationChanged;
            try
            {
                lifetimeCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            lifetimeCts.Dispose();
            debounceCts?.Cancel();
            debounceCts?.Dispose();
            debounceCts = null;
        }
    }
}
