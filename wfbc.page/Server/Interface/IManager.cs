using wfbc.page.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace wfbc.page.Server.Interface
{
    public interface IManager
    {
        public List<Manager> GetAllManagers();
        public Manager GetManager(string id);
        public void AddManager(Manager manager);
        public void UpdateManager(Manager manager);
        public void DeleteManager(string id);
    }
}
