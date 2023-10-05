using System;
using Microsoft.AspNetCore.Components;
using mhd.Domain;
using mhd.DataAccess;
using Microsoft.JSInterop;
using mhd.Pages;
using Blazorise;

namespace mhd.Pages
{
    public class AircraftBase : ComponentBase
    {
        [Inject]
        protected IMHDService MHDService { get; set; }
        protected List<mhd.Domain.Aircraft> aircraftList { get; set; }
        protected Modal aircraftMissionsModalRef;
        protected mhd.Domain.Aircraft SelectedAirCraft { get; set; } = new Domain.Aircraft();
        protected Domain.Aircraft missionCrewSummaries {get; set; } = new Domain.Aircraft
        {
            Mission = new List<Mission>()
        };
        protected override async Task OnInitializedAsync()
        {

            aircraftList = await MHDService.QueryAircraftAsync();

        }
        protected async Task ShowModal(mhd.Domain.Aircraft SelectedAirCraftRow)
        {
            if (SelectedAirCraft != null)
            {
                SelectedAirCraft = SelectedAirCraftRow;
                missionCrewSummaries = await MHDService.LoadAircraftMissionCrewSummaryAsync(SelectedAirCraftRow.acAircraftNo);
                await aircraftMissionsModalRef.Show();
            }
        }
        protected Task HideModal()
        {
            return aircraftMissionsModalRef.Hide();
        }

    }

}