using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wfbc.page.Shared.Models;
using wfbc.page.Server.Interface;
using MongoDB.Driver;

namespace wfbc.page.Server.DataAccess
{
    public class ManagerDataAccessLayer : IManager
    {
        private WfbcDBContext _db;
        public ManagerDataAccessLayer(WfbcDBContext db)
        {
            _db = db;
        }

        // Get all managers
        public List<Manager> GetAllManagers()
        {
            try
            {
                return _db.Managers.Find(_ => true).ToList();
            }
            catch
            {
                throw;
            }
        }
        // Get one manger
        public Manager GetManager(string id)
        {
            try
            {
                FilterDefinition<Manager> managerData = Builders<Manager>.Filter.Eq("Id", id);
                return _db.Managers.Find(managerData).FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }
        // Add a manager
        public void AddManager(Manager manager)
        {
            try
            {
                _db.Managers.InsertOne(manager);
            }
            catch
            {
                throw;
            }
        }
        // Update a manager
        public void UpdateManager(Manager manager)
        {
            try
            {
                _db.Managers.ReplaceOne(filter: m => m.Id == manager.Id, replacement: manager);
            }
            catch
            {
                throw;
            }
        }
        // Delete a manager
        public void DeleteManager(string id)
        {
            try
            {
                FilterDefinition<Manager> managerData = Builders<Manager>.Filter.Eq("Id", id);
                _db.Managers.DeleteOne(managerData);
            }
            catch
            {
                throw;
            }
        }
    }
}
