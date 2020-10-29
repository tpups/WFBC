using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wfbc.page.Shared.Models;
using wfbc.page.Server.Interface;
using MongoDB.Driver;

namespace wfbc.page.Server.DataAccess
{
    public class wfbcDataAccessLayer : IPick
    {
        private WfbcDBContext db;
        public wfbcDataAccessLayer(WfbcDBContext _db)
        {
            db = _db;
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
    }
}
