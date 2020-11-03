using wfbc.page.Shared.Models;
using System.Collections.Generic;

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
