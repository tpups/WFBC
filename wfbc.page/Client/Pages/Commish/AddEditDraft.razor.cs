using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;

namespace WFBC.Client.Pages.Commish
{
    public class AddEditDraftModel : CommishModel
    {
        protected string Title = "Create";
        [Parameter]
        public string draftID { get; set; }
        protected List<Pick> picks =new List<Pick>();
        protected override async Task OnParametersSetAsync()
        {
            if (!String.IsNullOrEmpty(draftID))
            {
                Title = "Edit";
                draft = await AuthorizedClient.Client.GetFromJsonAsync<Draft>("/api/draft" + draftID);
            }
            else
            {
                draft = new Draft();
                picks = new List<Pick>();
            }
        }
        protected async Task SaveDraft()
        {
            if (draft.Id != null)
            {
                await AuthorizedClient.Client.PutAsJsonAsync("/api/draft", draft);
            }
            else if (draft.Rounds != 0)
            {
                //draft.Picks = new List<string>();
                List<Manager> _managers = managers.FindAll(m => m.Status == "active");
                List<string> pickIds = new List<string>();
                for (int i = 0; i < draft.Rounds; i++)
                {
                    foreach (var manager in _managers)
                    {
                        Pick pick = new Pick
                        {
                            Round = i + 1,
                            TeamId = manager.TeamId,
                            DraftId = draft.Id,
                        };
                        picks.Add(pick);
                        draft.Picks.Add(pick.Id);
                    }
                }
                await AuthorizedClient.Client.PostAsJsonAsync("/api/pick/", picks);
                await AuthorizedClient.Client.PostAsJsonAsync("/api/draft/", draft);
            }
            ToCommish();
        }
        public void ToCommish()
        {
            UrlNavigationManager.NavigateTo("/commish");
        }
    }
}
