using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http;

namespace WFBC.Client.Pages
{
    public class DraftPicksDataModel : ComponentBase
    {
        [Inject]
        public HttpClient Http { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await GetPicks();
        }
        protected async Task GetPicks()
        {

        }
    }
}