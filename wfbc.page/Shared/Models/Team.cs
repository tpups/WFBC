using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace WFBC.Shared.Models
{
    public class Team
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [Required]
        public DateTime? CreatedAt { get; set; }
        [Required]
        public DateTime? LastUpdatedAt { get; set; }
        [BsonRepresentation(BsonType.ObjectId)]
        public string? ManagerId { get; set; }
        [Required]
        public string? Name { get; set; }
    }
}
