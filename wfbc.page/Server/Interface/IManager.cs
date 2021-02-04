using WFBC.Shared.Models;
using System.Collections.Generic;

namespace WFBC.Server.Interface
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
