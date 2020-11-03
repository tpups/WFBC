using wfbc.page.Shared.Models;
using System.Collections.Generic;

namespace wfbc.page.Server.Interface
{
    public interface IPick
    {
        public void AddPick(Pick pick);
        public void AddPicks(List<Pick> picks);
        public void UpdatePick(Pick pick);
        public Pick GetPick(string id);
        public List<Pick> GetPicks(List<string> ids);
    }
}
