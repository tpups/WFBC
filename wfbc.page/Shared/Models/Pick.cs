using System;
using System.Collections.Generic;
using System.Text;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace wfbc.page.Shared.Models
{
    public class Pick
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        // Manager Id of the pick's original owner
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string ManagerId { get; set; }
        // Manager Id of the team that currently owns the pick
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string OwnerId { get; set; }
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string DraftId { get; set; }
        public int Round { get; set; }
        public string Player { get; set; }
        public string PlayerPosition { get; set; }
        public string PlayerMLBTeam { get; set; }
    }
}
