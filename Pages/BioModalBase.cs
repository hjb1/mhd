using System;
using Microsoft.AspNetCore.Components;
using mhd.Domain;
using mhd.DataAccess;
using Radzen.Blazor;
using Radzen;
using Microsoft.JSInterop;

namespace mhd.Pages
{
    public class BioModalBase : ComponentBase
    {
        [Inject]
        protected IMHDService MHDService { get; set; }
        [Inject]
        protected IJSRuntime JSRuntime { get; set; }
        protected Bio bioData;
        
    }
}