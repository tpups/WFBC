﻿using System;
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
        public string draftId { get; set; }
        protected Draft draft = new Draft();
        protected List<Pick> picks =new List<Pick>();
        protected override async Task OnParametersSetAsync()
        {
            if (!String.IsNullOrEmpty(draftId))
            {
                Title = "Edit";
                draft = await Http.Client.GetFromJsonAsync<Draft>("/api/draft" + draftId);
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
                await Http.Client.PutAsJsonAsync("/api/draft", draft);
            }
            else if (draft.Rounds != 0)
            {
                //draft.Picks = new List<string>();
                List<Manager> managers = manList.FindAll(m => m.Status == "active");
                draft.Picks = new List<string>();
                for (int i = 0; i < draft.Rounds; i++)
                {
                    foreach (var manager in managers)
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
                //await Http.Client.PostAsJsonAsync("/api/pick/", picks);
                await Http.Client.PostAsJsonAsync("/api/draft/", draft);
            }
            ToCommish();
        }
        public void ToCommish()
        {
            UrlNavigationManager.NavigateTo("/commish");
        }
    }
}
