using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using wfbc.page.Shared.Models;
using wfbc.page.Server.Models;
using wfbc.page.Server.Interface;
using MongoDB.Driver;

namespace wfbc.page.Server.DataAccess
{
    public class StandingsDataAccessLayer : IStandings
    {
        private WfbcDBContext db;
        public StandingsDataAccessLayer(WfbcDBContext _db)
        {
            db = _db;
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
