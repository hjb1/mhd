using System;
using Microsoft.AspNetCore.Components;
using mhd.Domain;
using mhd.DataAccess;
using Microsoft.JSInterop;
using mhd.Pages;
using Blazorise;

namespace mhd.Pages
{
    public class PersonnelBase : ComponentBase
    {
        [Inject]
        protected IMHDService MHDService { get; set; }
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }
        protected List<PersonnelSummary> PersonnelList { get; set; } = null;
        protected Bio bioData;
        protected Modal bioModalRef;
        protected PersonnelSummary SelectedPersonnel { get; set; }
        public PersonnelSummary SelectedPerson { get; set; }

        protected override async Task OnInitializedAsync()
        {

            PersonnelList = await MHDService.QueryPersonnelAsync();

        }
        protected async Task ShowModal(PersonnelSummary SelectedPersonnel)
        {
            if (SelectedPersonnel != null)
            {
                SelectedPerson = SelectedPersonnel;
                bioData = await MHDService.LoadBioAsync(SelectedPersonnel.PerIdentification);
                await bioModalRef.Show();
            }
        }
        protected Task HideModal()
    {
        return bioModalRef.Hide();
    }
    }
}