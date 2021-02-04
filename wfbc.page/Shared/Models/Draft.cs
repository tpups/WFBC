using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WFBC.Shared.Models
{
    public class Draft
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public List<string> Managers { get; set; }
        public List<string> Picks { get; set; }
        public string Year { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public int Rounds { get; set; }
    }
}
