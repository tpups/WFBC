using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wfbc.page.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http;

namespace wfbc.page.Client.Pages
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