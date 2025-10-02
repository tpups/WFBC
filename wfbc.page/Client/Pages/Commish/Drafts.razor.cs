using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using System.Linq;


namespace WFBC.Client.Pages.Commish
{
    public class DraftsModel : ComponentBase
    {
        [Inject]
        public AuthorizedClient AuthorizedClient { get; set; }
        [Inject]
        public PublicClient PublicClient { get; set; }
        [Inject]
        public NavigationManager UrlNavigationManager { get; set; }
        
        protected List<Draft> drafts = new List<Draft>();
        protected Draft draft = new Draft();

        protected override async Task OnInitializedAsync()
        {
            await GetAllDrafts();
        }

        protected async Task GetAllDrafts()
        {
            drafts = await PublicClient.Client.GetFromJsonAsync<List<Draft>>("api/draft");
        }

        protected void DeleteConfirm(string ID)
        {
            draft = drafts.FirstOrDefault(x => x.Id == ID);
        }
        
        protected async Task DeleteDraft(string draftID)
        {
            try
            {
                await AuthorizedClient.Client.DeleteAsync("api/draft/" + draftID);
                await GetAllDrafts();
            }
            catch (AccessTokenNotAvailableException e)
            {
                e.Redirect();
            }
            catch
            {

            }
        }
    }
}
