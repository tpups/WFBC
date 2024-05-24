using MongoDB.Driver;
using System.Collections.Generic;
using WFBC.Shared.Models;

namespace WFBC.Server.Models
{
    public class WfbcDBContext
    {
        private readonly IMongoDatabase _wfbcDatabase;
        private readonly Dictionary<string, IMongoDatabase> _dbs;
        private readonly IMongoDatabase _wfbc2011;
        private readonly IMongoDatabase _wfbc2012;
        private readonly IMongoDatabase _wfbc2013;
        private readonly IMongoDatabase _wfbc2014;
        private readonly IMongoDatabase _wfbc2015;
        private readonly IMongoDatabase _wfbc2016;
        private readonly IMongoDatabase _wfbc2017;
        private readonly IMongoDatabase _wfbc2018;
        private readonly IMongoDatabase _wfbc2019;
        private readonly IMongoDatabase _wfbc2020;
        private readonly IMongoDatabase _wfbc2021;
        private readonly IMongoDatabase _wfbc2022;
        private readonly IMongoDatabase _wfbc2023;

        private readonly MongoClient client;
        public WfbcDBContext(IDatabaseSettings settings)
        {
            _dbs = new Dictionary<string, IMongoDatabase>();
            client = new MongoClient(settings.ConnectionString);
            _wfbcDatabase = client.GetDatabase(settings.DatabaseName);

            _wfbc2011 = client.GetDatabase("wfbc2011");
            _dbs.Add("2011", _wfbc2011);
            _wfbc2012 = client.GetDatabase("wfbc2012");
            _dbs.Add("2012", _wfbc2012);
            _wfbc2013 = client.GetDatabase("wfbc2013");
            _dbs.Add("2013", _wfbc2013);
            _wfbc2014 = client.GetDatabase("wfbc2014");
            _dbs.Add("2014", _wfbc2014);
            _wfbc2015 = client.GetDatabase("wfbc2015");
            _dbs.Add("2015", _wfbc2015);
            _wfbc2016 = client.GetDatabase("wfbc2016");
            _dbs.Add("2016", _wfbc2016);
            _wfbc2017 = client.GetDatabase("wfbc2017");
            _dbs.Add("2017", _wfbc2017);
            _wfbc2018 = client.GetDatabase("wfbc2018");
            _dbs.Add("2018", _wfbc2018);
            _wfbc2019 = client.GetDatabase("wfbc2019");
            _dbs.Add("2019", _wfbc2019);
            _wfbc2020 = client.GetDatabase("wfbc2020");
            _dbs.Add("2020", _wfbc2020);
            _wfbc2021 = client.GetDatabase("wfbc2021");
            _dbs.Add("2021", _wfbc2021);
            _wfbc2022 = client.GetDatabase("wfbc2022");
            _dbs.Add("2022", _wfbc2022);
            _wfbc2023 = client.GetDatabase("wfbc2023");
            _dbs.Add("2023", _wfbc2023);

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
                for (int i = 2011;  i <= 2023; i++)
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
                for (int i = 2011; i <= 2023; i++)
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
