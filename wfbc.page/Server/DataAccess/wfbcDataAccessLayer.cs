using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wfbc.page.Shared.Models;
using MongoDB.Driver;

namespace wfbc.page.Server.DataAccess
{
    public class wfbcDataAccessLayer
    {
        wfbcDBContext db = new wfbcDBContext();
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
