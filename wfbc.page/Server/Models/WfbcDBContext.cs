using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Driver;
using wfbc.page.Shared.Models;
using MongoDB.Driver.Core.Connections;
using Microsoft.Extensions.Configuration;

namespace wfbc.page.Server.Models
{
    public class WfbcDBContext 
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly MongoClient client;
        public WfbcDBContext(IDatabaseSettings settings)
        {
            client = new MongoClient(settings.ConnectionString);
            _mongoDatabase = client.GetDatabase("wfbc");
        }

        public IMongoCollection<Draft> Drafts
        {
            get
            {
                return _mongoDatabase.GetCollection<Draft>("drafts");
            }
        }
        public IMongoCollection<Pick> Picks
        {
            get
            {
                return _mongoDatabase.GetCollection<Pick>("picks");
            }
        }
        public IMongoCollection<Manager> Managers
        {
            get
            {
                 return _mongoDatabase.GetCollection<Manager>("managers");
            }
        }
        public IMongoCollection<Standings> Standings
        {
            get
            {
                return _mongoDatabase.GetCollection<Standings>("standings");
            }
        }
        public IMongoCollection<Box> BoxScores
        {
            get
            {
                return _mongoDatabase.GetCollection<Box>("boxscores");
            }
        }
    }
}
