using System;
using Microsoft.AspNetCore.Components;
using mhd.Domain;
using mhd.DataAccess;

namespace mhd.Pages
{
    public class PersonnelBase : ComponentBase
    {
        [Inject]
        protected IMHDService MHDService { get; set; }
        protected List<PersonnelSummary> PersonnelList { get; set; } = null;
        protected override async Task OnInitializedAsync()
        {

            PersonnelList = await MHDService.QueryPersonnelAsync();

        }
    }
}