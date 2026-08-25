using System.Timers;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using mhd.Domain;
using Timer = System.Timers.Timer;

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
        protected IJSRuntime JSRuntime { get; set; } = default!;

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

        private Timer? debounce;

        protected IEnumerable<mhd.Domain.Aircraft> VisibleRows => View.Take(VisibleCap);

        protected override async Task OnInitializedAsync()
        {
            await ReloadAsync(invalidate: false);
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (ShowFullList || IsScanning || View.Count <= VisibleCap)
            {
                return;
            }

            try
            {
                await JSRuntime.InvokeAsync<int>("mhd.ping");
            }
            catch (InvalidOperationException)
            {
                return;
            }
            catch (JSDisconnectedException)
            {
                return;
            }

            ShowFullList = true;
            StateHasChanged();
        }

        protected async Task ReloadAsync(bool invalidate)
        {
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
                ApplyView();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Aircraft load failed");
                LoadError = $"Could not open Aircraft. {ex.GetType().Name}: {ex.Message}";
                aircraftList = new List<mhd.Domain.Aircraft>();
                View = new List<mhd.Domain.Aircraft>();
            }
            finally
            {
                IsScanning = false;
            }
        }

        protected void OnFilterInput(ChangeEventArgs e)
        {
            Filter = e.Value?.ToString() ?? string.Empty;
            debounce?.Stop();
            debounce?.Dispose();
            debounce = new Timer(160);
            debounce.AutoReset = false;
            debounce.Elapsed += async (_, _) => await InvokeAsync(() =>
            {
                ApplyView();
                StateHasChanged();
            });
            debounce.Start();
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

        protected async Task OpenSelectedAsync()
        {
            if (SelectedAirCraft?.acBG == "44th")
            {
                await ShowModal(SelectedAirCraft);
            }
        }

        protected async Task ShowModal(mhd.Domain.Aircraft selectedAirCraftRow)
        {
            SelectedAirCraft = selectedAirCraftRow;
            ExpandedMissionNo = null;
            missionCrewSummaries = new mhd.Domain.Aircraft
            {
                acAircraftNo = selectedAirCraftRow.acAircraftNo,
                Mission = new List<Mission>()
            };
            MissionsLoading = true;
            ShowMissionsDialog = true;
            await InvokeAsync(StateHasChanged);

            try
            {
                missionCrewSummaries = await MHDService.LoadAircraftMissionCrewSummaryAsync(selectedAirCraftRow.acAircraftNo);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Mission load failed for {Aircraft}", selectedAirCraftRow.acAircraftNo);
                missionCrewSummaries = new mhd.Domain.Aircraft
                {
                    acAircraftNo = selectedAirCraftRow.acAircraftNo,
                    Mission = new List<Mission>()
                };
            }
            finally
            {
                MissionsLoading = false;
                await InvokeAsync(StateHasChanged);
            }

            try
            {
                var personnel = await MHDService.QueryPersonnelAsync();
                PersonnelNames = personnel
                    .Where(p => !string.IsNullOrEmpty(p.PerIdentification))
                    .GroupBy(p => p.PerIdentification)
                    .ToDictionary(g => g.Key, g => FormatName(g.First()));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Personnel name lookup failed for missions");
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
            debounce?.Dispose();
        }
    }
}
