using System.Collections.Generic;

namespace WFBC.Shared.Models
{
    /// <summary>
    /// Represents a single player's box score data as received from Rotowire
    /// (raw JSON from Rotowire - flexible dictionary to handle varying stat fields)
    /// </summary>
    public class BoxScoreEntry : Dictionary<string, object?>
    {
    }

    /// <summary>
    /// Result of a box score import operation
    /// </summary>
    public class BoxScoreImportResult
    {
        public int NewEntries { get; set; }
        public int UpdatedEntries { get; set; }
        public int SkippedEntries { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Request payload for importing box scores from the client
    /// </summary>
    public class BoxScoreImportRequest
    {
        public string Year { get; set; } = string.Empty;
        public string TeamId { get; set; } = string.Empty;
        public string StatsDate { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "hitting" or "pitching"
        public List<Dictionary<string, object?>> Entries { get; set; } = new();
    }
}