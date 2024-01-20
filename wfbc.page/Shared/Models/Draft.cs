using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace WFBC.Shared.Models
{
    public class Draft
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [Required]
        public DateTime? CreatedAt { get; set; }

        [Required]
        public DateTime? LastUpdatedAt { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public List<string>? Teams { get; set; }

        [BsonRepresentation(BsonType.ObjectId)]
        public List<string>? Picks { get; set; }

        [Required]
        public int Year { get; set; }

        [Required]
        public string? DraftType { get; set; }

        public string? Type { get; set; }

        public DateTime? Date { get; set; }

        public int Rounds { get; set; }
    }
}
