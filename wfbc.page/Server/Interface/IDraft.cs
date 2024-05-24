using WFBC.Shared.Models;
using System.Collections.Generic;

namespace WFBC.Server.Interface
{
    public interface IDraft
    {
        public List<Draft> GetAllDrafts();
        public Draft GetDraft(string id);
        public string AddDraft(Draft draft);
        public void UpdateDraft(Draft drafts);
        public void DeleteDraft(string id);
    }
}
