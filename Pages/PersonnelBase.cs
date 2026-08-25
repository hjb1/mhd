using System.Timers;
using Blazorise;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using Microsoft.JSInterop;
using mhd.Domain;
using Timer = System.Timers.Timer;

namespace mhd.Pages
{
    public class PersonnelBase : ComponentBase, IDisposable
    {
        [Inject]
        protected IMHDService MHDService { get; set; } = default!;
        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        protected List<PersonnelSummary> PersonnelList { get; set; } = new();
        protected List<PersonnelSummary> View { get; set; } = new();
        protected Virtualize<PersonnelSummary>? PersonGrid;
        protected Bio? bioData;
        protected Modal bioModalRef = default!;
        protected PersonnelSummary? SelectedPerson { get; set; }
        protected string Filter { get; set; } = string.Empty;
        protected bool BiosOnly { get; set; }
        protected string SortColumn { get; set; } = "LastName";
        protected bool SortAscending { get; set; } = true;
        protected string? LoadError { get; set; }
        protected int BioTab { get; set; }
        protected bool IsLoading { get; set; } = true;
        protected bool IsScanning { get; set; } = true;

        private Timer? debounce;
        private CancellationTokenSource? loadCts;
        private bool pendingVirtualizeRefresh;
        private bool loadStarted;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!loadStarted)
            {
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

                loadStarted = true;
                await ReloadAsync(invalidate: false);
                return;
            }

            if (pendingVirtualizeRefresh && PersonGrid != null)
            {
                pendingVirtualizeRefresh = false;
                await PersonGrid.RefreshDataAsync();
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
            PersonnelList = new List<PersonnelSummary>();
            View = PersonnelList;
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

                await MHDService.FillPersonnelAsync(PersonnelList, progress, token);
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
                LoadError = "Could not open Personnel. The database may be unavailable.";
                PersonnelList = new List<PersonnelSummary>();
                View = new List<PersonnelSummary>();
            }
            finally
            {
                IsLoading = false;
                IsScanning = false;
                pendingVirtualizeRefresh = true;
            }
        }

        protected ValueTask<ItemsProviderResult<PersonnelSummary>> ProvidePersonnel(ItemsProviderRequest request)
        {
            var snapshot = View;
            if (snapshot.Count == 0 || request.StartIndex >= snapshot.Count)
            {
                return ValueTask.FromResult(new ItemsProviderResult<PersonnelSummary>(Array.Empty<PersonnelSummary>(), snapshot.Count));
            }

            var count = Math.Min(request.Count, snapshot.Count - request.StartIndex);
            return ValueTask.FromResult(new ItemsProviderResult<PersonnelSummary>(
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
                if (PersonGrid != null)
                {
                    await PersonGrid.RefreshDataAsync();
                }
                StateHasChanged();
            });
            debounce.Start();
        }

        protected void OnBiosOnlyChanged(ChangeEventArgs e)
        {
            BiosOnly = e.Value is bool flag
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

        protected void SelectRow(PersonnelSummary person)
        {
            SelectedPerson = person;
        }

        protected async Task OpenSelectedAsync()
        {
            if (SelectedPerson != null && (SelectedPerson.HasBio || HasObituary(SelectedPerson)))
            {
                await ShowModal(SelectedPerson);
            }
        }

        protected async Task ShowModal(PersonnelSummary selectedPersonnel)
        {
            SelectedPerson = selectedPersonnel;
            BioTab = 0;
            bioData = await MHDService.LoadBioAsync(selectedPersonnel.PerIdentification);
            await bioModalRef.Show();
        }

        protected Task HideModal()
        {
            return bioModalRef.Hide();
        }

        protected static bool HasObituary(PersonnelSummary person) =>
            !string.IsNullOrWhiteSpace(person.ObituaryComments);

        protected static string Display(string? value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value;

        private void ApplyView(bool duringScan)
        {
            var hasFilter = BiosOnly || !string.IsNullOrWhiteSpace(Filter);
            if (duringScan && !hasFilter && SortColumn == "LastName" && SortAscending)
            {
                View = PersonnelList;
                return;
            }

            IEnumerable<PersonnelSummary> query = PersonnelList;

            if (BiosOnly)
            {
                query = query.Where(p => p.HasBio || HasObituary(p));
            }

            if (!string.IsNullOrWhiteSpace(Filter))
            {
                var term = Filter.Trim();
                query = query.Where(p =>
                    Contains(p.LastName, term) ||
                    Contains(p.FirstName, term) ||
                    Contains(p.PerIdentification, term) ||
                    Contains(p.PerGroup, term) ||
                    Contains(p.PerSquadron, term));
            }

            query = (SortColumn, SortAscending) switch
            {
                ("PerIdentification", true) => query.OrderBy(p => p.PerIdentification),
                ("PerIdentification", false) => query.OrderByDescending(p => p.PerIdentification),
                ("FirstName", true) => query.OrderBy(p => p.FirstName),
                ("FirstName", false) => query.OrderByDescending(p => p.FirstName),
                ("HasBio", true) => query.OrderByDescending(p => p.HasBio).ThenBy(p => p.LastName),
                ("HasBio", false) => query.OrderBy(p => p.HasBio).ThenBy(p => p.LastName),
                ("PerGroup", true) => query.OrderBy(p => p.PerGroup),
                ("PerGroup", false) => query.OrderByDescending(p => p.PerGroup),
                ("PerSquadron", true) => query.OrderBy(p => p.PerSquadron),
                ("PerSquadron", false) => query.OrderByDescending(p => p.PerSquadron),
                ("DeceasedDate", true) => query.OrderBy(p => p.DeceasedDate),
                ("DeceasedDate", false) => query.OrderByDescending(p => p.DeceasedDate),
                ("LastName", false) => query.OrderByDescending(p => p.LastName).ThenByDescending(p => p.FirstName),
                _ => query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            };

            View = query.ToList();
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
