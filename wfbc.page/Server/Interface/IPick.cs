using wfbc.page.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wfbc.page.Server.Interface
{
    public interface IPick
    {
        public void AddPick(Pick pick);
        public void AddPicks(List<Pick> picks);
        public void UpdatePick(Pick pick);
    }
}
