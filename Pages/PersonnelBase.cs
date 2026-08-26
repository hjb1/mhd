using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;
using mhd.Domain;

namespace mhd.Pages
{
    public class PersonnelBase : ComponentBase, IDisposable
    {
        protected const int VisibleCap = 300;

        [Inject]
        protected IMHDService MHDService { get; set; } = default!;
        [Inject]
        protected ILogger<PersonnelBase> Logger { get; set; } = default!;
        [Inject]
        protected NavigationManager Nav { get; set; } = default!;
        [Inject]
        protected IJSRuntime JSRuntime { get; set; } = default!;

        protected string? SelectId { get; set; }
        protected int FindEpoch { get; set; }

        protected List<PersonnelSummary> PersonnelList { get; set; } = new();
        protected List<PersonnelSummary> View { get; set; } = new();
        protected Bio? bioData;
        protected PersonnelSummary? SelectedPerson { get; set; }
        protected bool ShowBioDialog { get; set; }
        protected bool BioLoading { get; set; }
        protected string Filter { get; set; } = string.Empty;
        protected bool BiosOnly { get; set; }
        protected bool KiaOnly { get; set; }
        protected string SortColumn { get; set; } = "LastName";
        protected bool SortAscending { get; set; } = true;
        protected string? LoadError { get; set; }
        protected int BioTab { get; set; }
        protected bool IsScanning { get; set; } = true;
        protected bool ShowFullList { get; set; }

        private CancellationTokenSource? debounceCts;
        private readonly CancellationTokenSource lifetimeCts = new();
        private bool disposed;
        private string? appliedSelect;
        private bool pendingScroll;

        protected IEnumerable<PersonnelSummary> VisibleRows => View.Take(VisibleCap);

        protected override async Task OnInitializedAsync()
        {
            Nav.LocationChanged += OnLocationChanged;
            await ReloadAsync(invalidate: false);
        }

        protected override Task OnParametersSetAsync()
        {
            ApplyIncomingSelect();
            return Task.CompletedTask;
        }

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            _ = InvokeAsync(() =>
            {
                ApplyIncomingSelect();
                StateHasChanged();
            });
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender && !disposed && string.IsNullOrWhiteSpace(appliedSelect))
            {
                ApplyIncomingSelect();
                if (!string.IsNullOrWhiteSpace(appliedSelect))
                {
                    await InvokeAsync(StateHasChanged);
                }
            }

            if (!pendingScroll || disposed || string.IsNullOrWhiteSpace(appliedSelect))
            {
                return;
            }

            pendingScroll = false;
            try
            {
                await JSRuntime.InvokeAsync<int>("mhd.scrollPerson", appliedSelect);
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
                PersonnelList = await MHDService.QueryPersonnelAsync();
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

                Logger.LogError(ex, "Personnel load failed");
                LoadError = $"Could not open Personnel. {ex.GetType().Name}: {ex.Message}";
                PersonnelList = new List<PersonnelSummary>();
                View = new List<PersonnelSummary>();
            }
            finally
            {
                if (!disposed)
                {
                    IsScanning = false;
                    ApplyIncomingSelect();
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

        protected void OnBiosOnlyChanged(ChangeEventArgs e)
        {
            BiosOnly = e.Value is bool flag
                ? flag
                : string.Equals(e.Value?.ToString(), "true", StringComparison.OrdinalIgnoreCase);
            ApplyView();
        }

        protected void OnKiaOnlyChanged(ChangeEventArgs e)
        {
            KiaOnly = e.Value is bool flag
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

        protected void SelectRow(PersonnelSummary person)
        {
            SelectedPerson = person;
        }

        private void ApplyIncomingSelect()
        {
            SelectId = QueryString.Get(Nav, "select") ?? SelectId;
            if (disposed || PersonnelList.Count == 0 || string.IsNullOrWhiteSpace(SelectId))
            {
                return;
            }

            if (appliedSelect == SelectId && SelectedPerson?.PerIdentification == SelectId)
            {
                pendingScroll = true;
                return;
            }

            var wanted = SelectId.Trim();
            var person = PersonnelList.FirstOrDefault(p =>
                string.Equals((p.PerIdentification ?? string.Empty).Trim(), wanted, StringComparison.OrdinalIgnoreCase));
            BiosOnly = false;
            KiaOnly = false;
            if (person == null)
            {
                Filter = wanted;
                FindEpoch++;
                ApplyView();
                Logger.LogWarning("Deep-link personnel {Id} was not in the list", wanted);
                return;
            }

            Filter = person.PerIdentification;
            SelectedPerson = person;
            appliedSelect = SelectId;
            FindEpoch++;
            ApplyView();
            pendingScroll = true;
            Logger.LogInformation("Selected personnel from query {Id}", person.PerIdentification);
        }

        protected Task OpenBioById(string id)
        {
            var person = PersonnelList.FirstOrDefault(p => p.PerIdentification == id);
            return person == null ? Task.CompletedTask : ShowModal(person);
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
            bioData = null;
            BioLoading = true;
            ShowBioDialog = true;
            await SafeStateHasChangedAsync();

            try
            {
                bioData = await MHDService.LoadBioAsync(selectedPersonnel.PerIdentification);
                if (disposed)
                {
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Bio load failed for {Id}", selectedPersonnel.PerIdentification);
                if (disposed)
                {
                    return;
                }

                bioData = null;
            }
            finally
            {
                if (!disposed)
                {
                    BioLoading = false;
                    await SafeStateHasChangedAsync();
                }
            }
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

        protected void HideModal()
        {
            ShowBioDialog = false;
        }

        protected static bool HasObituary(PersonnelSummary person) =>
            !string.IsNullOrWhiteSpace(person.ObituaryComments);

        protected static string Display(string? value) =>
            Bio.IsMeaningfulValue(value) ? value!.Trim() : string.Empty;

        private void ApplyView()
        {
            IEnumerable<PersonnelSummary> query = PersonnelList;

            if (BiosOnly)
            {
                query = query.Where(p => p.HasBio || HasObituary(p));
            }

            if (KiaOnly)
            {
                query = query.Where(p => p.HasKia);
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
                ("HasKia", true) => query.OrderByDescending(p => p.HasKia).ThenBy(p => p.LastName),
                ("HasKia", false) => query.OrderBy(p => p.HasKia).ThenBy(p => p.LastName),
                ("PerGroup", true) => query.OrderBy(p => p.PerGroup),
                ("PerGroup", false) => query.OrderByDescending(p => p.PerGroup),
                ("PerSquadron", true) => query.OrderBy(p => p.PerSquadron),
                ("PerSquadron", false) => query.OrderByDescending(p => p.PerSquadron),
                ("DeceasedDate", true) => query.OrderBy(p => p.DeceasedDate),
                ("DeceasedDate", false) => query.OrderByDescending(p => p.DeceasedDate),
                ("Obituary", true) => query.OrderByDescending(p => HasObituary(p)).ThenBy(p => p.LastName),
                ("Obituary", false) => query.OrderBy(p => HasObituary(p)).ThenBy(p => p.LastName),
                ("LastName", false) => query.OrderByDescending(p => p.LastName).ThenByDescending(p => p.FirstName),
                _ => query.OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            };

            View = query.ToList();
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
