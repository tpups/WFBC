using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WFBC.Shared.Models
{
    public class Standings
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        [Required]
        public DateTime? CreatedAt { get; set; }
        [Required]
        public DateTime? LastUpdatedAt { get; set; }
        public string Id { get; set; }
        public string Year { get; set; }
        public DateTime Date { get; set; }
    }
}
