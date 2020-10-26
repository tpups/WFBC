using wfbc.page.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wfbc.page.Server.Interface
{
    public interface IWfbc
    {
        public List<Manager> GetAllManagers();
        public void AddManager(Manager manager);
        public void UpdateManager(Manager manager);
        public void DeleteManager(string id);
        public void AddPick(Pick pick);
        public void AddPicks(List<Pick> picks);
        public void UpdatePick(Pick pick);
        public Standings GetStandingsByDate(DateTime date);
    }
}
