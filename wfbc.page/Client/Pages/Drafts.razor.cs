using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wfbc.page.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;

namespace wfbc.page.Client.Pages
{
    public class DraftsModel : ComponentBase
    {
        [Inject]
        public HttpClient Http { get; set; }
        protected List<Draft> drafts = new List<Draft>();
        protected Draft draft = new Draft();
        [Inject]
        public NavigationManager UrlNavigationManager { get; set; }
        [Parameter]
        public string draftId { get; set; }
        protected override async Task OnInitializedAsync()
        {
            await GetAllDrafts();
        }
        protected async Task GetAllDrafts()
        {
            drafts = await Http.GetFromJsonAsync<List<Draft>>("api/draft");
        }
        protected async Task GetDraft(string id)
        {
            draft = await Http.GetFromJsonAsync<Draft>("api/draft/" + draftId);
        }
        protected async Task SaveDraft()
        {
            if (draftId != null)
            {
                await Http.PutAsJsonAsync("api/draft/", draft);
            }
            else
            {
                await Http.PostAsJsonAsync("api/draft/", draft);
            }
        }
    }
}
