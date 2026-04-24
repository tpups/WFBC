using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WFBC.Shared.Models
{
    /// <summary>
    /// Represents a single wager comparison between players
    /// </summary>
    public class WagerData
    {
        /// <summary>
        /// Display name for the wager (e.g., "KG vs KMcG")
        /// </summary>
        [BsonElement("wager_name")]
        public string WagerName { get; set; } = string.Empty;

        /// <summary>
        /// Subtitle describing the wager criteria (e.g., "Counting Stats (R, RBI, HR, SB, BB)")
        /// </summary>
        [BsonElement("subtitle")]
        public string Subtitle { get; set; } = string.Empty;

        /// <summary>
        /// The stat categories being compared
        /// </summary>
        [BsonElement("stat_categories")]
        public List<string> StatCategories { get; set; } = new();

        /// <summary>
        /// The players being compared
        /// </summary>
        [BsonElement("players")]
        public List<WagerPlayer> Players { get; set; } = new();
    }

    /// <summary>
    /// Represents a player's stats in a wager comparison
    /// </summary>
    public class WagerPlayer
    {
        /// <summary>
        /// Display name for the player
        /// </summary>
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Rotowire newsID for the player
        /// </summary>
        [BsonElement("news_id")]
        public string NewsId { get; set; } = string.Empty;

        /// <summary>
        /// Total combined value of all stat categories
        /// </summary>
        [BsonElement("value")]
        public int Value { get; set; }

        /// <summary>
        /// Individual stat values keyed by category name
        /// </summary>
        [BsonElement("stat_breakdown")]
        public Dictionary<string, int> StatBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Compiled wager data document - one per season
    /// Stored in compiled_standings collection alongside other compiled docs
    /// </summary>
    public class CompiledWagerData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("year")]
        public string Year { get; set; } = string.Empty;

        [BsonElement("type")]
        public string Type { get; set; } = "wager_data";

        [BsonElement("compiled_at")]
        public DateTime CompiledAt { get; set; }

        /// <summary>
        /// When the box scores were last downloaded/imported (max download_date across player box scores)
        /// </summary>
        [BsonElement("last_box_score_update")]
        [BsonIgnoreIfNull]
        public DateTime? LastBoxScoreUpdate { get; set; }

        [BsonElement("wagers")]
        public List<WagerData> Wagers { get; set; } = new();
    }
}