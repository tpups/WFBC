using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WFBC.Shared.Models
{
    /// <summary>
    /// Compiled final standings document - one per season
    /// Replaces need to query thousands of individual standings documents
    /// </summary>
    public class CompiledFinalStandings
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        /// <summary>
        /// Year for this compiled data (e.g., "2023")
        /// </summary>
        [BsonElement("year")]
        public string Year { get; set; } = string.Empty;

        /// <summary>
        /// Type identifier for this document
        /// </summary>
        [BsonElement("type")]
        public string Type { get; set; } = "final_standings";

        /// <summary>
        /// When this compiled data was generated
        /// </summary>
        [BsonElement("compiled_at")]
        public DateTime CompiledAt { get; set; }

        /// <summary>
        /// When the source standings data was last updated
        /// </summary>
        [BsonElement("source_last_updated")]
        public DateTime? SourceLastUpdated { get; set; }

        /// <summary>
        /// Final standings for each team (latest date)
        /// </summary>
        [BsonElement("final_standings")]
        public List<Standings> FinalStandings { get; set; } = new List<Standings>();

        /// <summary>
        /// Metadata about the compilation process
        /// </summary>
        [BsonElement("metadata")]
        public CompilationMetadata Metadata { get; set; } = new CompilationMetadata();
    }

    /// <summary>
    /// Compiled progression data document - one per season
    /// Contains time series data for charts
    /// </summary>
    public class CompiledProgressionData
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        /// <summary>
        /// Year for this compiled data (e.g., "2023")
        /// </summary>
        [BsonElement("year")]
        public string Year { get; set; } = string.Empty;

        /// <summary>
        /// Type identifier for this document
        /// </summary>
        [BsonElement("type")]
        public string Type { get; set; } = "progression_data";

        /// <summary>
        /// When this compiled data was generated
        /// </summary>
        [BsonElement("compiled_at")]
        public DateTime CompiledAt { get; set; }

        /// <summary>
        /// When the source standings data was last updated
        /// </summary>
        [BsonElement("source_last_updated")]
        public DateTime? SourceLastUpdated { get; set; }

        /// <summary>
        /// All standings data ordered by date for progression display
        /// </summary>
        [BsonElement("progression_data")]
        public List<Standings> ProgressionData { get; set; } = new List<Standings>();

        /// <summary>
        /// Metadata about the compilation process
        /// </summary>
        [BsonElement("metadata")]
        public CompilationMetadata Metadata { get; set; } = new CompilationMetadata();
    }

    /// <summary>
    /// Metadata about the compilation process
    /// </summary>
    public class CompilationMetadata
    {
        /// <summary>
        /// Total number of source documents processed
        /// </summary>
        [BsonElement("source_documents_processed")]
        public int SourceDocumentsProcessed { get; set; }

        /// <summary>
        /// Number of teams included
        /// </summary>
        [BsonElement("teams_count")]
        public int TeamsCount { get; set; }

        /// <summary>
        /// Date range of the source data
        /// </summary>
        [BsonElement("date_range_start")]
        public DateTime? DateRangeStart { get; set; }

        /// <summary>
        /// Date range of the source data
        /// </summary>
        [BsonElement("date_range_end")]
        public DateTime? DateRangeEnd { get; set; }

        /// <summary>
        /// How long the compilation took
        /// </summary>
        [BsonElement("compilation_duration_ms")]
        public long CompilationDurationMs { get; set; }

        /// <summary>
        /// Version of the compilation logic used
        /// </summary>
        [BsonElement("compilation_version")]
        public string CompilationVersion { get; set; } = "1.0";
    }

    /// <summary>
    /// Response wrapper for compiled standings API calls
    /// </summary>
    public class CompiledStandingsResponse<T>
    {
        /// <summary>
        /// The compiled data
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// When the compiled data was generated
        /// </summary>
        public DateTime? CompiledAt { get; set; }

        /// <summary>
        /// When the source data was last updated
        /// </summary>
        public DateTime? SourceLastUpdated { get; set; }

        /// <summary>
        /// Cache key for client-side caching
        /// </summary>
        public string CacheKey { get; set; } = string.Empty;

        /// <summary>
        /// Whether this came from compiled documents (true) or fallback (false)
        /// </summary>
        public bool IsCompiled { get; set; } = true;

        /// <summary>
        /// Metadata about the data source
        /// </summary>
        public CompilationMetadata? Metadata { get; set; }
    }
}
