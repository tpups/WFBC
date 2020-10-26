using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Driver;

namespace wfbc.page.Shared.Models
{
    public class WfbcDBContext 
    {
        private readonly IMongoDatabase _mongoDatabase;

        public WfbcDBContext()
        {

            var client = new MongoClient("mongodb+srv://admin:%2A7a3TLJ3aI%23t@cluster0.nfj4j.mongodb.net/<dbname>?retryWrites=true&w=majority");
            //var database = client.GetDatabase("test");
            //var client = new MongoClient("mongodb://localhost:27017");
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
