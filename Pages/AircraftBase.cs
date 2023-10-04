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

        protected override async Task OnInitializedAsync()
        {

            aircraftList = await MHDService.QueryAircraftAsync();

        }
        
    }

}