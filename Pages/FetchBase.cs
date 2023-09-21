using System;
using mhd.Bio;
using Microsoft.AspNetCore.Components;

namespace mhd.Pages
{
    public class FetchBase : ComponentBase
    {
        [Inject]
        protected IBioService BioService { get; set; }
        protected List<BioSummary> BiosList { get; set; } = null;
        protected override async Task OnInitializedAsync()
        {

            BiosList = await BioService.QueryDocumentAsync();

        }
    }
}