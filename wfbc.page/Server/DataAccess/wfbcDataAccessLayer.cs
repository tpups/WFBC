using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wfbc.page.Shared.Models;
using wfbc.page.Server.Interface;
using MongoDB.Driver;

namespace wfbc.page.Server.DataAccess
{
    public class wfbcDataAccessLayer : IWfbc
    {
        private wfbcDBContext db;
        public wfbcDataAccessLayer(wfbcDBContext _db)
        {
            db = _db;
        }

        // Get all managers
        public List<Manager> GetAllManagers()
        {
            try
            {
                return db.Managers.Find(_ => true).ToList();
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
                db.Managers.InsertOne(manager);
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
                db.Managers.ReplaceOne(filter: m => m.Id == manager.Id, replacement: manager);
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
                db.Managers.DeleteOne(managerData);
            }
            catch
            {
                throw;
            }
        }
        // Add a draft pick
        public void AddPick(Pick pick)
        {
            try
            {
                db.Picks.InsertOne(pick);
            }
            catch
            {
                throw;
            }
        }
        // Add a list of draft picks
        public void AddPicks(List<Pick> picks)
        {
            try
            {
                db.Picks.InsertMany(picks);
            }
            catch
            {
                throw;
            }
        }
        // Update a draft pick
        public void UpdatePick(Pick pick)
        {
            try
            {
                db.Picks.ReplaceOne(filter: p => p.Id == pick.Id, replacement: pick);
            }
            catch
            {
                throw;
            }
        }
        // Get the standings on a specific day
        public Standings GetStandingsByDate(DateTime date)
        {
            try
            {
                FilterDefinition<Standings> filterStandingsData = Builders<Standings>.Filter.Eq("Date", date);
                return db.Standings.Find(filterStandingsData).FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }
    }
}
