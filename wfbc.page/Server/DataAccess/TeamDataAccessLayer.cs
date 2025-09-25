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
    public class TeamDataAccessLayer : ITeam
    {
        private WfbcDBContext _db;
        public TeamDataAccessLayer(WfbcDBContext db)
        {
            _db = db;
        }

        // Get all Teams
        public List<Team> GetAllTeams()
        {
            try
            {
                return _db.Teams.Find(_ => true).ToList();
            }
            catch
            {
                throw;
            }
        }
        // Get one team
        public Team GetTeam(string id)
        {
            try
            {
                FilterDefinition<Team> teamData = Builders<Team>.Filter.Eq("Id", id);
                return _db.Teams.Find(teamData).FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }
        // Add a team
        public void AddTeam(Team team)
        {
            try
            {
                _db.Teams.InsertOne(team);
            }
            catch
            {
                throw;
            }
        }
        // Update a team
        public void UpdateTeam(Team team)
        {
            try
            {
                _db.Teams.ReplaceOne(filter: m => m.Id == team.Id, replacement: team);
            }
            catch
            {
                throw;
            }
        }
        // Delete a manager
        public void DeleteTeam(string id)
        {
            try
            {
                FilterDefinition<Team> teamData = Builders<Team>.Filter.Eq("Id", id);
                _db.Teams.DeleteOne(teamData);
            }
            catch
            {
                throw;
            }
        }

        // Get teams for a specific season/year
        public List<SeasonTeam> GetTeamsForSeason(string year)
        {
            try
            {
                var collection = _db.SeasonTeams[year];
                return collection.Find(_ => true).ToList();
            }
            catch
            {
                throw;
            }
        }
    }
}
