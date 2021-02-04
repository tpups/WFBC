using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WFBC.Shared.Models
{
    public class Pick
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        // Manager Id of the pick's original owner
        public string ManagerId { get; set; }
        // Manager Id of the team that currently owns the pick
        public string OwnerId { get; set; }
        public string DraftId { get; set; }
        public int Round { get; set; }
        public string Player { get; set; }
        public string PlayerPosition { get; set; }
        public string PlayerMLBTeam { get; set; }
    }
}
