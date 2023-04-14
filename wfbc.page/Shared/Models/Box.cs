using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WFBC.Shared.Models
{
    public class Box
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string ManagerId { get; set; }
        public DateOnly Date { get; set; }

        public class HittingBox
        {
            //Hitting
            public int? GamesPlayed { get; set; }
            public int? PlateAppearances { get; set; }
            public int? AtBats { get; set; }
            public int? Hits { get; set; }
            public int? Doubles { get; set; }
            public int? Triples { get; set; }
            public int? HomeRuns { get; set; }
            public int? Walks { get; set; }
            public int? Strikeouts { get; set; }
            public int? HitByPitch { get; set; }
            public int? StolenBases { get; set; }
            public int? CaughtStealing { get; set; }
            public int? Runs { get; set; }
            public int? RunsBattedIn { get; set; }
            public int? SacrificeFlies { get; set; }

            //public decimal? Average { get; set; }
            //public decimal? OnBasePlusSlugging { get; set; }
        }

        public class PitchingBox
        {
            public int? GamesPlayed { get; set; }
            public int? InningsPitched { get; set; }
            public int? Hits { get; set; }
            public int? Strikeouts { get; set; }
            public int? Walks { get; set; }
            public int? HitBatters { get; set; }
            public int? Runs { get; set; }
            public int? EarnedRuns { get; set; }
            public int? HomeRuns { get; set; }
            public int? WildPitches { get; set; }
            public int? QualityStarts { get; set; }
            public int? Saves { get; set; }
            public int? BlownSaves { get; set; }
            public int? Wins { get; set; }
            public int? Losses { get; set; }

            //public decimal? EarnedRunAverage { get; set; }
            //public decimal? WalksAndHitsPerInningPitched { get; set; }
        }
    }
}
