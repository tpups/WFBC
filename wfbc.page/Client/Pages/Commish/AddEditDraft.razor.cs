using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wfbc.page.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;

namespace wfbc.page.Client.Pages.Commish
{
    public class AddEditDraftModel : CommishModel
    {
        protected string Title = "Create";
        [Parameter]
        public string draftId { get; set; }
        protected Draft draft = new Draft();
        protected override async Task OnParametersSetAsync()
        {
            if (!String.IsNullOrEmpty(draftId))
            {
                Title = "Edit";
                draft = await Http.GetFromJsonAsync<Draft>("/api/draft" + draftId);
            }
            else
            {
                draft = new Draft();
            }
        }
        protected async Task SaveDraft()
        {
            if (draft.Id != null)
            {
                await Http.PutAsJsonAsync("/api/draft", draft);
            }
            else if (draft.Rounds != 0)
            {
                draft.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
                draft.Picks = new List<string>();
                List<Manager> managers = manList.FindAll(m => m.Status == "active");
                List<Pick> picks = new List<Pick>();
                for (int i = 0; i < draft.Rounds; i++)
                {
                    foreach (var manager in managers)
                    {
                        Pick pick = new Pick
                        {
                            Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
                            Round = i + 1,
                            ManagerId = manager.Id,
                            DraftId = draft.Id,
                        };
                        picks.Add(pick);
                        draft.Picks.Add(pick.Id);
                    }
                }
                await Http.PostAsJsonAsync("/api/pick/", picks);
                await Http.PostAsJsonAsync("/api/draft/", draft);
            }
            ToCommish();
        }
        public void ToCommish()
        {
            UrlNavigationManager.NavigateTo("/commish");
        }
    }
}
