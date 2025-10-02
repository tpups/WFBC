using System;

namespace WFBC.Shared.Models
{
    public class StandingsResponse<T>
    {
        public T Data { get; set; }
        public DateTime? LastUpdated { get; set; }
        public string CacheKey { get; set; }
    }
    
    public class StandingsInfo
    {
        public bool Exist { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int RecordCount { get; set; }
    }
}
