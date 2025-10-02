using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace WFBC.Shared.Models
{
    public class SeasonSettings
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("year")]
        public int Year { get; set; }

        [BsonElement("seasonStartDate")]
        public DateTime SeasonStartDate { get; set; }

        [BsonElement("seasonEndDate")]
        public DateTime SeasonEndDate { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Default constructor for MongoDB serialization
        public SeasonSettings() { }

        // Constructor with default dates (March 1 - October 31)
        public SeasonSettings(int year)
        {
            Year = year;
            SeasonStartDate = new DateTime(year, 3, 1); // March 1st
            SeasonEndDate = new DateTime(year, 10, 31); // October 31st
        }
    }
}
