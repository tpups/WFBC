using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace wfbc.page.Shared.Models
{
    public class Draft
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public List<Manager> Managers { get; set; }
        public string Year { get; set; }
        public string Type { get; set; }
        public DateTime Date { get; set; }
        public int Teams { get; set; }
    }
}
