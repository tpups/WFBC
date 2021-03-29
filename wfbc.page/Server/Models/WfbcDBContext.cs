using MongoDB.Driver;
using WFBC.Shared.Models;

namespace WFBC.Server.Models
{
    public class WfbcDBContext 
    {
        private readonly IMongoDatabase _mongoDatabase;
        private readonly MongoClient client;
        public WfbcDBContext(IDatabaseSettings settings)
        {
            client = new MongoClient(settings.ConnectionString);
            _mongoDatabase = client.GetDatabase(settings.DatabaseName);
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
