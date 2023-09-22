using System;
using mhd.DataAccess;
using mhd.Domain;
using Microsoft.AspNetCore.Components;

namespace mhd.Pages
{
    public class FetchBase : ComponentBase
    {
        [Inject]
        protected IMHDService MHDService { get; set; }
        protected List<BioSummary> BiosList { get; set; } = null;
        protected override async Task OnInitializedAsync()
        {

            BiosList = await MHDService.QueryDocumentAsync();

        }
    }
}