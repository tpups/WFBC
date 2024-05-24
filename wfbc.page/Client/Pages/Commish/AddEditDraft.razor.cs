using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using Microsoft.AspNetCore.Components;
using System.Net.Http;
using System.Net.Http.Json;
using Amazon.Runtime.Internal.Transform;
using MongoDB.Bson.IO;
using System.Text.Json.Serialization;
using System.Text.Json;
using MongoDB.Bson;

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
                draft = await AuthorizedClient.Client.GetFromJsonAsync<Draft>("/api/draft/" + draftID);
            }
            else
            {
                draft = new Draft();
                picks = new List<Pick>();
            }
        }
        protected async Task<string> SaveDraft()
        {
            if (draft.Id != null)
            {
                draft.LastUpdatedAt = DateTime.Now;
                await AuthorizedClient.Client.PutAsJsonAsync("/api/draft", draft);
            }
            else if (draft.Rounds != 0)
            {
                draft.CreatedAt = DateTime.Now;
                draft.LastUpdatedAt = draft.CreatedAt;
                HttpResponseMessage draftResponse = await AuthorizedClient.Client.PostAsJsonAsync("/api/draft/", draft);
                string newDraftID = await draftResponse.Content.ReadAsStringAsync();

                List<Manager> _managers = managers.FindAll(m => m.Status == "Active");

                DateTime timestamp = DateTime.Now;

                for (int i = 0; i < draft.Rounds; i++)
                {
                    foreach (var manager in _managers)
                    {
                        if (string.IsNullOrWhiteSpace(manager.TeamId))
                        {
                            return "Not all active managers are assigned to a team. Please update using Teams page.";
                        }
                        Pick pick = new Pick
                        {
                            CreatedAt = timestamp,
                            LastUpdatedAt = timestamp,
                            Round = i + 1,
                            Year = draft.Year,
                            DraftType = draft.DraftType,
                            TeamId = manager.TeamId,
                            DraftId = newDraftID
                        };
                        picks.Add(pick);
                    }
                }

                HttpResponseMessage picksResponse = await AuthorizedClient.Client.PostAsJsonAsync("/api/pick/", picks);
                string _newPickIDs = await picksResponse.Content.ReadAsStringAsync();
                List<string> newPickIDs = JsonSerializer.Deserialize<string[]>(_newPickIDs).ToList();

                draft = await AuthorizedClient.Client.GetFromJsonAsync<Draft>("/api/draft/" + newDraftID);
                draft.Picks = newPickIDs;
                HttpResponseMessage draftAddPicksResponse = await AuthorizedClient.Client.PutAsJsonAsync("/api/draft/", draft);
            }
            UrlNavigationManager.NavigateTo("/commish/drafts");
            return "success";
        }
    }
}
