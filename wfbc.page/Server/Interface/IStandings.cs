using WFBC.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WFBC.Server.Interface
{
    public interface IStandings
    {
        public Standings GetStandingsByDate(DateTime date);
        public Task SaveStandingsAsync(List<Standings> standings, string year);
        public Task<StandingsInfo> GetExistingStandingsInfoAsync(string year);
        public Task DeleteAllStandingsForYearAsync(string year);
        public Task<bool> StandingsExistForYearAsync(string year);
    }
    
    public class StandingsInfo
    {
        public bool Exist { get; set; }
        public DateTime? LastUpdated { get; set; }
        public int RecordCount { get; set; }
    }
}
