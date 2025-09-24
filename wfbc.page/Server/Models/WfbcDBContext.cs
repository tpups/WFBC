using MongoDB.Driver;
using System.Collections.Generic;
using WFBC.Shared.Models;

namespace WFBC.Server.Models
{
    public class WfbcDBContext
    {
        private readonly IMongoDatabase _wfbcDatabase;
        private readonly Dictionary<string, IMongoDatabase> _dbs;
        private readonly MongoClient client;
        private readonly IDatabaseSettings _settings;
        public WfbcDBContext(IDatabaseSettings settings)
        {
            _settings = settings;
            _dbs = new Dictionary<string, IMongoDatabase>();
            client = new MongoClient(settings.ConnectionString);
            _wfbcDatabase = client.GetDatabase(settings.DatabaseName);

            // Dynamic year database initialization
            for (int year = settings.StartYear; year <= settings.EndYear; year++)
            {
                string yearStr = year.ToString();
                var yearDb = client.GetDatabase($"wfbc{yearStr}");
                _dbs.Add(yearStr, yearDb);
            }
        }

        public IMongoCollection<Draft> Drafts
        {
            get
            {
                return _wfbcDatabase.GetCollection<Draft>("drafts");
            }
        }
        public IMongoCollection<Pick> Picks
        {
            get
            {
                return _wfbcDatabase.GetCollection<Pick>("picks");
            }
        }
        public IMongoCollection<Manager> Managers
        {
            get
            {
                 return _wfbcDatabase.GetCollection<Manager>("managers");
            }
        }
        public IMongoCollection<Team> Teams
        {
            get
            {
                return _wfbcDatabase.GetCollection<Team>("teams");
            }
        }
        public Dictionary<string, IMongoCollection<Standings>> Standings
        {
            get
            {
                Dictionary<string, IMongoCollection<Standings>> _standings = new Dictionary<string, IMongoCollection<Standings>>();
                for (int i = _settings.StartYear; i <= _settings.EndYear; i++)
                {
                    string year = i.ToString();
                    _standings.Add(year, _dbs[year].GetCollection<Standings>("standings"));
                }
                return _standings;
            }
        }
        public Dictionary<string, Dictionary<string, IMongoCollection<Box>>> BoxScores
        {
            get
            {
                Dictionary<string, Dictionary<string, IMongoCollection<Box>>> _boxScores = new Dictionary<string, Dictionary<string, IMongoCollection<Box>>>();
                for (int i = _settings.StartYear; i <= _settings.EndYear; i++)
                {
                    string year = i.ToString();
                    _boxScores.Add(year, new Dictionary<string, IMongoCollection<Box>>
                    {
                        { "hitting", _dbs[year].GetCollection<Box>("team_box_hitting") },
                        { "pitching", _dbs[year].GetCollection<Box>("team_box_pitching") },
                    });
                }
                return _boxScores;
            }
        }
    }
}
