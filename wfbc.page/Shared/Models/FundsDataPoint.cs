using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WFBC.Shared.Models
{
    /// <summary>
    /// Represents a single data point for "The Funds" chart.
    /// Each document stores the total fund amount at a given date.
    /// Stored in the "the_funds" collection in the year-specific database.
    /// </summary>
    public class FundsDataPoint
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("date")]
        public DateTime Date { get; set; }

        [BsonElement("amount")]
        public decimal Amount { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Response wrapper for funds data including season context
    /// </summary>
    public class FundsResponse
    {
        public List<FundsDataPoint> DataPoints { get; set; } = new();
        public DateTime SeasonStartDate { get; set; }
    }
}