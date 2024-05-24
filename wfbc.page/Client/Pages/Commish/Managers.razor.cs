using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using System.Linq;


namespace WFBC.Client.Pages.Commish
{
    public class ManagersModel : CommishModel
    {
        protected void DeleteConfirm(string ID)
        {
            manager = managers.FirstOrDefault(x => x.Id == ID);
        }
        protected async Task DeleteManager(string managerID)
        {
            try
            {
                await AuthorizedClient.Client.DeleteAsync("api/manager/" + managerID);
                await GetAllManagers();
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
