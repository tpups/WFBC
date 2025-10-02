using WFBC.Shared.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WFBC.Server.Interface
{
    public interface IPick
    {
        public string AddPick(Pick pick);
        public Task<string[]> AddPicks(List<Pick> picks);
        public void UpdatePick(Pick pick);
        public Pick GetPick(string id);
        public List<Pick> GetPicks(List<string> ids);
        public void DeletePick(string id);
    }
}
