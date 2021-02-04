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
