using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace WFBC.Shared.Models
{
    public class Pick
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        // Team Id of the pick's original team
        [Required]
        public string TeamId { get; set; }
        // Team Id of the team that currently owns the pick
        public string? OwnerTeamId { get; set; }
        [Required]
        public string DraftId { get; set; }
        [Required]
        public int Round { get; set; }
        public string? Player { get; set; }
        public string? PlayerPosition { get; set; }
        public string? PlayerMLBTeam { get; set; }
    }
}
