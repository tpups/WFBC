using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace WFBC.Shared.Models
{
    public class Box
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        // Team and player identification
        [BsonElement("teamID")]
        public string? TeamId { get; set; }

        [BsonElement("player")]
        public string? Player { get; set; }

        [BsonElement("firstname")]
        public string? FirstName { get; set; }

        [BsonElement("lastname")]
        public string? LastName { get; set; }

        [BsonElement("newsID")]
        public string? NewsId { get; set; }

        [BsonElement("injury")]
        public string? Injury { get; set; }

        [BsonElement("slot")]
        public string? Slot { get; set; }

        [BsonElement("team")]
        public string? Team { get; set; }

        [BsonElement("position")]
        public string? Position { get; set; }

        [BsonElement("dnp")]
        public string? DidNotPlay { get; set; }

        [BsonElement("opponent")]
        public string? Opponent { get; set; }

        [BsonElement("result")]
        public string? Result { get; set; }

        [BsonElement("gameID")]
        public string? GameId { get; set; }

        [BsonElement("gameDate")]
        public string? GameDate { get; set; }

        [BsonElement("currentlyBatting")]
        public string? CurrentlyBatting { get; set; }

        [BsonElement("currentlyPitching")]
        public string? CurrentlyPitching { get; set; }

        [BsonElement("onBase")]
        public string? OnBase { get; set; }

        [BsonElement("confirmed")]
        public string? Confirmed { get; set; }

        [BsonElement("stats_date")]
        public string? StatsDate { get; set; }

        [BsonElement("download_date")]
        public string? DownloadDate { get; set; }

        // Hitting stats (for team_box_hitting collection)
        [BsonElement("PA")]
        public object? PlateAppearances { get; set; }

        [BsonElement("AB")]
        public object? AtBats { get; set; }

        [BsonElement("H")]
        public object? Hits { get; set; }

        [BsonElement("2B")]
        public object? Doubles { get; set; }

        [BsonElement("3B")]
        public object? Triples { get; set; }

        [BsonElement("HR")]
        public object? HomeRuns { get; set; }

        [BsonElement("R")]
        public object? Runs { get; set; }

        [BsonElement("RBI")]
        public object? RunsBattedIn { get; set; }

        [BsonElement("SB")]
        public object? StolenBases { get; set; }

        [BsonElement("CS")]
        public object? CaughtStealing { get; set; }

        [BsonElement("BB")]
        public object? Walks { get; set; }

        [BsonElement("K")]
        public object? Strikeouts { get; set; }

        [BsonElement("HBP")]
        public object? HitByPitch { get; set; }

        [BsonElement("SF")]
        public object? SacrificeFlies { get; set; }

        [BsonElement("AVG")]
        public string? Average { get; set; }

        // Handle 2019 case sensitivity issue where batting average is stored as "Avg" instead of "AVG"
        [BsonElement("Avg")]
        [BsonIgnoreIfNull]
        public string? Average2019 
        { 
            get => Average; 
            set => Average = value ?? Average; 
        }

        [BsonElement("OPS")]
        public string? OnBasePlusSlugging { get; set; }

        // Pitching stats (for team_box_pitching collection)
        [BsonElement("IP")]
        public string? InningsPitched { get; set; }

        [BsonElement("ER")]
        public object? EarnedRuns { get; set; }

        [BsonElement("QS")]
        public object? QualityStarts { get; set; }

        [BsonElement("S")]
        public object? Saves { get; set; } // For 2020+

        [BsonElement("SV")]
        public object? SavesAlternate { get; set; } // For 2019

        [BsonElement("HB")]
        public object? HitBatters { get; set; }

        [BsonElement("GP")]
        public object? GamesPlayed { get; set; }

        [BsonElement("GS")]
        public object? GamesStarted { get; set; }

        [BsonElement("W")]
        public object? Wins { get; set; }

        [BsonElement("L")]
        public object? Losses { get; set; }

        [BsonElement("BS")]
        public object? BlownSaves { get; set; }

        [BsonElement("WP")]
        public object? WildPitches { get; set; }

        [BsonElement("ERA")]
        public string? EarnedRunAverage { get; set; }

        [BsonElement("WHIP")]
        public string? WalksAndHitsPerInningPitched { get; set; }

        [BsonElement("points")]
        public object? Points { get; set; }

        [BsonElement("updates")]
        public List<string>? Updates { get; set; }

        [BsonElement("pitches")]
        public object? Pitches { get; set; }
    }
}
