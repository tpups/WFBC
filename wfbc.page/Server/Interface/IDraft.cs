using wfbc.page.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wfbc.page.Server.Interface
{
    public interface IDraft
    {
        public List<Draft> GetAllDrafts();
        public Draft GetDraft(string id);
        public void AddDraft(Draft draft);
        public void UpdateDraft(Draft drafts);
        public void DeleteDraft(string id);
    }
}
