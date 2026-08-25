using System.Timers;
using Blazorise;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using mhd.Domain;
using Timer = System.Timers.Timer;

namespace mhd.Pages
{
    public class AircraftBase : ComponentBase, IDisposable
    {
        [Inject]
        protected IMHDService MHDService { get; set; } = default!;

        protected List<mhd.Domain.Aircraft> aircraftList { get; set; } = new();
        protected List<mhd.Domain.Aircraft> View { get; set; } = new();
        protected Virtualize<mhd.Domain.Aircraft>? AircraftGrid;
        protected Modal aircraftMissionsModalRef = default!;
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
        protected bool IsLoading { get; set; } = true;
        protected bool IsScanning { get; set; } = true;
        protected bool MissionsLoading { get; set; }

        private Timer? debounce;
        private CancellationTokenSource? loadCts;
        private bool pendingVirtualizeRefresh;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await ReloadAsync(invalidate: false);
                return;
            }

            if (pendingVirtualizeRefresh && AircraftGrid != null)
            {
                pendingVirtualizeRefresh = false;
                await AircraftGrid.RefreshDataAsync();
            }
        }

        protected async Task ReloadAsync(bool invalidate)
        {
            loadCts?.Cancel();
            loadCts?.Dispose();
            loadCts = new CancellationTokenSource();
            var token = loadCts.Token;

            IsLoading = true;
            IsScanning = true;
            LoadError = null;
            aircraftList = new List<mhd.Domain.Aircraft>();
            View = aircraftList;
            try
            {
                if (invalidate)
                {
                    MHDService.InvalidateListCache();
                }

                var progress = new Progress<int>(count =>
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    IsLoading = false;
                    ApplyView(duringScan: true);
                    pendingVirtualizeRefresh = true;
                    InvokeAsync(StateHasChanged);
                });

                await MHDService.FillAircraftAsync(aircraftList, progress, token);
                if (token.IsCancellationRequested)
                {
                    return;
                }

                ApplyView(duringScan: false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception)
            {
                LoadError = "Could not open Aircraft. The database may be unavailable.";
                aircraftList = new List<mhd.Domain.Aircraft>();
                View = new List<mhd.Domain.Aircraft>();
            }
            finally
            {
                IsLoading = false;
                IsScanning = false;
                pendingVirtualizeRefresh = true;
            }
        }

        protected ValueTask<ItemsProviderResult<mhd.Domain.Aircraft>> ProvideAircraft(ItemsProviderRequest request)
        {
            var snapshot = View;
            if (snapshot.Count == 0 || request.StartIndex >= snapshot.Count)
            {
                return ValueTask.FromResult(new ItemsProviderResult<mhd.Domain.Aircraft>(Array.Empty<mhd.Domain.Aircraft>(), snapshot.Count));
            }

            var count = Math.Min(request.Count, snapshot.Count - request.StartIndex);
            return ValueTask.FromResult(new ItemsProviderResult<mhd.Domain.Aircraft>(
                snapshot.GetRange(request.StartIndex, count),
                snapshot.Count));
        }

        protected void OnFilterInput(ChangeEventArgs e)
        {
            Filter = e.Value?.ToString() ?? string.Empty;
            debounce?.Stop();
            debounce?.Dispose();
            debounce = new Timer(160);
            debounce.AutoReset = false;
            debounce.Elapsed += async (_, _) => await InvokeAsync(async () =>
            {
                ApplyView(duringScan: IsScanning);
                if (AircraftGrid != null)
                {
                    await AircraftGrid.RefreshDataAsync();
                }
                StateHasChanged();
            });
            debounce.Start();
        }

        protected void OnOnly44thChanged(ChangeEventArgs e)
        {
            Only44th = e.Value is bool flag
                ? flag
                : string.Equals(e.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
            ApplyView(duringScan: IsScanning);
            pendingVirtualizeRefresh = true;
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

            ApplyView(duringScan: false);
            pendingVirtualizeRefresh = true;
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
            MissionsLoading = true;
            await aircraftMissionsModalRef.Show();

            try
            {
                var missionsTask = MHDService.LoadAircraftMissionCrewSummaryAsync(selectedAirCraftRow.acAircraftNo);
                var personnelTask = MHDService.QueryPersonnelAsync();
                await Task.WhenAll(missionsTask, personnelTask);

                missionCrewSummaries = missionsTask.Result;
                PersonnelNames = personnelTask.Result
                    .Where(p => !string.IsNullOrEmpty(p.PerIdentification))
                    .GroupBy(p => p.PerIdentification)
                    .ToDictionary(g => g.Key, g => FormatName(g.First()));
            }
            finally
            {
                MissionsLoading = false;
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

        protected Task HideModal()
        {
            return aircraftMissionsModalRef.Hide();
        }

        private void ApplyView(bool duringScan)
        {
            var hasFilter = Only44th || !string.IsNullOrWhiteSpace(Filter);
            if (duringScan && !hasFilter && SortColumn == "acAircraftNo" && SortAscending)
            {
                View = aircraftList;
                return;
            }

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
            loadCts?.Cancel();
            loadCts?.Dispose();
            debounce?.Dispose();
        }
    }
}
