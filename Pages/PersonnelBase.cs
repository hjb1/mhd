using System.Timers;
using Blazorise;
using Microsoft.AspNetCore.Components;
using mhd.Domain;
using Timer = System.Timers.Timer;

namespace mhd.Pages
{
    public class PersonnelBase : ComponentBase, IDisposable
    {
        protected const int VisibleCap = 300;

        [Inject]
        protected IMHDService MHDService { get; set; } = default!;
        [Inject]
        protected ILogger<PersonnelBase> Logger { get; set; } = default!;

        protected List<PersonnelSummary> PersonnelList { get; set; } = new();
        protected List<PersonnelSummary> View { get; set; } = new();
        protected Bio? bioData;
        protected Modal bioModalRef = default!;
        protected PersonnelSummary? SelectedPerson { get; set; }
        protected string Filter { get; set; } = string.Empty;
        protected bool BiosOnly { get; set; }
        protected string SortColumn { get; set; } = "LastName";
        protected bool SortAscending { get; set; } = true;
        protected string? LoadError { get; set; }
        protected int BioTab { get; set; }
        protected bool IsScanning { get; set; } = true;

        private Timer? debounce;

        protected IEnumerable<PersonnelSummary> VisibleRows => View.Take(VisibleCap);

        protected override async Task OnInitializedAsync()
        {
            await ReloadAsync(invalidate: false);
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

                PersonnelList = await MHDService.QueryPersonnelAsync();
                ApplyView();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Personnel load failed");
                LoadError = $"Could not open Personnel. {ex.GetType().Name}: {ex.Message}";
                PersonnelList = new List<PersonnelSummary>();
                View = new List<PersonnelSummary>();
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

        protected void OnBiosOnlyChanged(ChangeEventArgs e)
        {
            BiosOnly = e.Value is bool flag
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

        private void ApplyView()
        {
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
            debounce?.Dispose();
        }
    }
}
