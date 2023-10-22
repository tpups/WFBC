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

        [Required]
        public DateTime? CreatedAt { get; set; }

        [Required]
        public DateTime? LastUpdatedAt { get; set; }

        // Team Id of the pick's original team
        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? TeamId { get; set; }

        // Team Id of the team that currently owns the pick
        [BsonRepresentation(BsonType.ObjectId)]
        public string? OwnerTeamId { get; set; }

        [Required]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? DraftId { get; set; }

        [Required]
        public int Round { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public string? DraftType { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string? Player { get; set; }
    }
}
