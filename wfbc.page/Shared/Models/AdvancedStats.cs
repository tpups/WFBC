using MongoDB.Bson.Serialization.Attributes;

namespace WFBC.Shared.Models
{
    /// <summary>
    /// Advanced statistics bucket, kept intentionally separate from the core scoring
    /// categories on <see cref="Standings"/>.
    ///
    /// All current stats are derived from the "active lineup" team totals
    /// (position == "A", player == "TOT") that drive the rotisserie standings.
    /// Fields can be added here in the future (e.g. inactive-lineup stats) without
    /// disturbing the scoring category properties on <see cref="Standings"/>.
    /// </summary>
    public class AdvancedStats
    {
        // --- Pitching ---

        /// <summary>
        /// Sum of pitcher Games Started (GS) for the team's active lineup.
        /// </summary>
        [BsonElement("starts")]
        public int Starts { get; set; }

        /// <summary>
        /// Quality Start rate = QS / GS (guarded for division by zero).
        /// </summary>
        [BsonElement("qualityStartRate")]
        public decimal QualityStartRate { get; set; }

        /// <summary>
        /// Raw quality starts carried over for transparency / future use.
        /// </summary>
        [BsonElement("qualityStarts")]
        public int QualityStarts { get; set; }

        // --- Batting ---

        /// <summary>
        /// Strikeout rate (K%) for batters = K / PA (guarded for division by zero).
        /// </summary>
        [BsonElement("strikeoutRateBatting")]
        public decimal StrikeoutRateBatting { get; set; }

        /// <summary>
        /// Walk rate (BB%) for batters = BB / PA (guarded for division by zero).
        /// </summary>
        [BsonElement("walkRateBatting")]
        public decimal WalkRateBatting { get; set; }

        /// <summary>
        /// Raw batting strikeouts (K) summed across active hitters.
        /// </summary>
        [BsonElement("battingK")]
        public int BattingStrikeouts { get; set; }

        /// <summary>
        /// Raw batting walks (BB) summed across active hitters.
        /// </summary>
        [BsonElement("battingBB")]
        public int BattingWalks { get; set; }

        /// <summary>
        /// Raw batting plate appearances (PA) summed across active hitters.
        /// </summary>
        [BsonElement("battingPA")]
        public int BattingPlateAppearances { get; set; }

        // --- Stolen Base Success ---

        /// <summary>
        /// Stolen base success rate = SB / (SB + CS) (guarded for division by zero).
        /// </summary>
        [BsonElement("stolenBaseSuccessRate")]
        public decimal StolenBaseSuccessRate { get; set; }

        /// <summary>
        /// Raw stolen bases (SB) summed across active hitters.
        /// </summary>
        [BsonElement("battingSB")]
        public int BattingStolenBases { get; set; }

        /// <summary>
        /// Raw caught stealing (CS) summed across active hitters.
        /// </summary>
        [BsonElement("battingCS")]
        public int BattingCaughtStealing { get; set; }

        // --- BABIP ---

        /// <summary>
        /// Batting average on balls in play = (H - HR) / (AB - K - HR + SF).
        /// </summary>
        [BsonElement("babip")]
        public decimal BABIP { get; set; }

        /// <summary>
        /// Raw batting hits (H) summed across active hitters.
        /// </summary>
        [BsonElement("battingH")]
        public int BattingHits { get; set; }

        /// <summary>
        /// Raw batting home runs (HR) summed across active hitters.
        /// </summary>
        [BsonElement("battingHR")]
        public int BattingHomeRuns { get; set; }

        /// <summary>
        /// Raw batting at bats (AB) summed across active hitters.
        /// </summary>
        [BsonElement("battingAB")]
        public int BattingAtBats { get; set; }

        /// <summary>
        /// Raw batting sacrifice flies (SF) summed across active hitters.
        /// </summary>
        [BsonElement("battingSF")]
        public int BattingSacrificeFlies { get; set; }
    }
}
