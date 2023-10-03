using System;
using Microsoft.AspNetCore.Components;
using mhd.Domain;
using mhd.DataAccess;
using Radzen.Blazor;
using Radzen;
using Microsoft.JSInterop;

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
        protected override async Task OnInitializedAsync()
        {

            PersonnelList = await MHDService.QueryPersonnelAsync();

        }
        protected async void ShowBioModal(PersonnelSummary personnel)
        {
            bioData = await MHDService.LoadBioAsync(personnel.PerIdentification);

            await JSRuntime.InvokeAsync<object>("showBioModal");
        }

    }
}