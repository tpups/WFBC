using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace WFBC.Shared.Models
{
    public class Manager
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required]
        public DateTime? CreatedAt { get; set; }

        [Required]
        public DateTime? LastUpdatedAt { get; set; }

        public string? Name { get; set; }

        [Required]
        public string? FirstName { get; set; }

        [Required]
        public string? LastName { get; set; }

        [Required]
        public string? TeamName { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public string? TeamId { get; set; }

        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? Status { get; set; }

        [Required]
        public string? Access { get; set; }

        public Dictionary<string, string>? RotowireTeamIds { get; set; }
    }
}
