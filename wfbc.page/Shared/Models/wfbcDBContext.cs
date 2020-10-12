using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Driver;

namespace wfbc.page.Shared.Models
{
    public class wfbcDBContext 
    {
        private readonly IMongoDatabase _mongoDatabase;

        public wfbcDBContext()
        {
            var client = new MongoClient("mongodb://localhost:27017");
            _mongoDatabase = client.GetDatabase("wfbc");
        }

        public IMongoCollection<Draft> Drafts
        {
            get
            {
                return _mongoDatabase.GetCollection<Draft>("Drafts");
            }
        }
        public IMongoCollection<Pick> Picks
        {
            get
            {
                return _mongoDatabase.GetCollection<Pick>("Picks");
            }
        }
        public IMongoCollection<Manager> Managers
        {
            get
            {
                return _mongoDatabase.GetCollection<Manager>("Managers");
            }
        }
        public IMongoCollection<Standings> Standings
        {
            get
            {
                return _mongoDatabase.GetCollection<Standings>("Standings");
            }
        }
        public IMongoCollection<Box> BoxScores
        {
            get
            {
                return _mongoDatabase.GetCollection<Box>("BoxScores");
            }
        }
    }
}
