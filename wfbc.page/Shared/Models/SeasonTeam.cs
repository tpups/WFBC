using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace WFBC.Shared.Models
{
    public class SeasonTeam
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("manager")]
        public string? Manager { get; set; }

        [BsonElement("team_id")]
        public string? TeamId { get; set; }

        [BsonElement("team_name")]
        public string? TeamName { get; set; }
    }
}
