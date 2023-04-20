using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using System.Linq;


namespace WFBC.Client.Pages.Commish
{
    public class DraftsModel : CommishModel
    {
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
