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
        
        // New methods for display functionality
        public Task<List<Standings>> GetFinalStandingsForYearAsync(string year);
        public Task<List<Standings>> GetStandingsProgressionForYearAsync(string year);
        public Task<DateTime?> GetStandingsLastUpdatedAsync(string year);
        
        // Fast existence checking methods
        public Task<bool> DatabaseExistsAsync(string year);
        public Task<bool> CollectionExistsAsync(string year);
    }
}
