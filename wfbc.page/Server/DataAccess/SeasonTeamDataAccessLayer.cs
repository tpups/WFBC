using System;
using System.Collections.Generic;
using WFBC.Shared.Models;
using WFBC.Server.Models;
using WFBC.Server.Interface;
using MongoDB.Driver;
using MongoDB.Bson;

namespace WFBC.Server.DataAccess
{
    public class SeasonTeamDataAccessLayer : ISeasonTeam
    {
        private readonly WfbcDBContext _db;
        public SeasonTeamDataAccessLayer(WfbcDBContext db) { _db = db; }

        public List<SeasonTeam> GetTeamsForYear(string year)
            => _db.SeasonTeams[year].Find(_ => true).ToList();

        public SeasonTeam GetSeasonTeam(string year, string id)
        {
            var filter = Builders<SeasonTeam>.Filter.Eq("_id", ObjectId.Parse(id));
            return _db.SeasonTeams[year].Find(filter).FirstOrDefault();
        }

        public void AddSeasonTeam(string year, SeasonTeam team)
        {
            team.Year = int.Parse(year);
            _db.SeasonTeams[year].InsertOne(team);
        }

        public void UpdateSeasonTeam(string year, SeasonTeam team)
            => _db.SeasonTeams[year].ReplaceOne(t => t.Id == team.Id, team);

        public void DeleteSeasonTeam(string year, string id)
        {
            var filter = Builders<SeasonTeam>.Filter.Eq("_id", ObjectId.Parse(id));
            _db.SeasonTeams[year].DeleteOne(filter);
        }
    }
}