using WFBC.Shared.Models;
using System.Collections.Generic;

namespace WFBC.Server.Interface
{
    public interface IPick
    {
        public string AddPick(Pick pick);
        public string[] AddPicks(List<Pick> picks);
        public void UpdatePick(Pick pick);
        public Pick GetPick(string id);
        public List<Pick> GetPicks(List<string> ids);
    }
}
