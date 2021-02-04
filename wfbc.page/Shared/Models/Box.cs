using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WFBC.Shared.Models
{
    public class Box
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
    }
}
