using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using WFBC.Server.Models;
using WFBC.Server.Interface;
using MongoDB.Driver;

namespace WFBC.Server.DataAccess
{
    public class PickDataAccessLayer : IPick
    {
        private WfbcDBContext _db;
        public PickDataAccessLayer(WfbcDBContext db)
        {
            _db = db;
        }

        public List<Pick> GetPicks(List<string> picks)
        {
            try
            {
                var picksData = new FilterDefinitionBuilder<Pick>();
                var picksFilter = picksData.In(p => p.Id, picks);
                return _db.Picks.Find(picksFilter).ToList();
            }
            catch
            {
                throw;
            }
        }
        public Pick GetPick(string id)
        {
            try
            {
                FilterDefinition<Pick> pickData = Builders<Pick>.Filter.Eq("Id", id);
                return _db.Picks.Find(pickData).FirstOrDefault();
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
                _db.Picks.InsertOne(pick);
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
                _db.Picks.InsertMany(picks);
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
                _db.Picks.ReplaceOne(filter: p => p.Id == pick.Id, replacement: pick);
            }
            catch
            {
                throw;
            }
        }
    }
}
