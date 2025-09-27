using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WFBC.Shared.Models;

namespace WFBC.Client.Services
{
    public class StandingsCacheService
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, StandingsCache> _cache;
        private readonly TimeSpan _maxCacheAge = TimeSpan.FromHours(1); // Fallback expiration

        public StandingsCacheService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _cache = new Dictionary<string, StandingsCache>();
        }

        // Get final standings with simplified caching
        public async Task<List<Standings>> GetFinalStandingsAsync(string year)
        {
            var cacheKey = $"final_{year}";
            
            // Simple cache check - only use cache if it's recent and has data
            if (_cache.ContainsKey(cacheKey))
            {
                var cachedData = _cache[cacheKey];
                var cacheAge = DateTime.UtcNow - cachedData.CachedAt;
                
                // Use cache if it's less than 5 minutes old (simple time-based invalidation)
                if (cacheAge < TimeSpan.FromMinutes(5))
                {
                    Console.WriteLine($"[StandingsCache] Serving final standings for {year} from cache ({cachedData.FinalStandings?.Count ?? 0} records, {cacheAge.TotalMinutes:F1}min old)");
                    return cachedData.FinalStandings ?? new List<Standings>();
                }
                else
                {
                    Console.WriteLine($"[StandingsCache] Cache expired for {year} ({cacheAge.TotalMinutes:F1}min old) - fetching fresh data");
                    _cache.Remove(cacheKey);
                }
            }
            else
            {
                Console.WriteLine($"[StandingsCache] No cache exists for {year}");
            }

            // Fetch fresh data from server
            Console.WriteLine($"[StandingsCache] Fetching fresh final standings for {year}");
            try
            {
                var response = await _httpClient.GetFromJsonAsync<StandingsResponse<List<Standings>>>($"api/Standings/final/{year}");
                
                if (response?.Data != null)
                {
                    Console.WriteLine($"[StandingsCache] Server returned {response.Data.Count} final standings for {year}");
                    
                    // Cache the data with simple timestamp
                    _cache[cacheKey] = new StandingsCache
                    {
                        Year = year,
                        CachedAt = DateTime.UtcNow,
                        DataLastUpdated = response.LastUpdated ?? DateTime.UtcNow,
                        FinalStandings = response.Data,
                        ProgressionData = null // Not fetched yet
                    };

                    return response.Data;
                }
                else
                {
                    Console.WriteLine($"[StandingsCache] Server returned null/empty response for {year}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StandingsCache] Error fetching final standings for {year}: {ex.Message}");
            }

            Console.WriteLine($"[StandingsCache] Returning empty list for {year}");
            return new List<Standings>();
        }

        // Get progression data with caching
        public async Task<List<Standings>> GetProgressionDataAsync(string year)
        {
            var cacheKey = $"final_{year}"; // Use same cache entry
            
            // Check if we already have progression data cached
            if (_cache.ContainsKey(cacheKey) && _cache[cacheKey].ProgressionData != null)
            {
                var cachedData = _cache[cacheKey];
                
                // Check if cache is fresh
                var serverLastModified = await GetServerLastModifiedAsync(year);
                
                if (cachedData.DataLastUpdated >= serverLastModified && 
                    DateTime.UtcNow - cachedData.CachedAt < _maxCacheAge)
                {
                    Console.WriteLine($"[StandingsCache] Serving progression data for {year} from cache");
                    return cachedData.ProgressionData;
                }
                else
                {
                    Console.WriteLine($"[StandingsCache] Cache invalidated for {year} - server data is newer");
                    _cache.Remove(cacheKey);
                }
            }

            // Fetch fresh data from server
            Console.WriteLine($"[StandingsCache] Fetching fresh progression data for {year}");
            try
            {
                var response = await _httpClient.GetFromJsonAsync<StandingsResponse<List<Standings>>>($"api/Standings/progression/{year}");
                
                if (response?.Data != null)
                {
                    // Update or create cache entry
                    if (_cache.ContainsKey(cacheKey))
                    {
                        _cache[cacheKey].ProgressionData = response.Data;
                        _cache[cacheKey].DataLastUpdated = response.LastUpdated ?? DateTime.UtcNow;
                        _cache[cacheKey].CachedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _cache[cacheKey] = new StandingsCache
                        {
                            Year = year,
                            CachedAt = DateTime.UtcNow,
                            DataLastUpdated = response.LastUpdated ?? DateTime.UtcNow,
                            FinalStandings = null, // Not fetched yet
                            ProgressionData = response.Data
                        };
                    }

                    return response.Data;
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("timeout") || ex.Message.Contains("canceled"))
            {
                Console.WriteLine($"[StandingsCache] Timeout fetching progression data for {year} - collection likely doesn't exist");
                // Cache empty result to prevent repeated attempts
                if (_cache.ContainsKey(cacheKey))
                {
                    _cache[cacheKey].ProgressionData = new List<Standings>();
                }
                else
                {
                    _cache[cacheKey] = new StandingsCache
                    {
                        Year = year,
                        CachedAt = DateTime.UtcNow,
                        DataLastUpdated = DateTime.MinValue,
                        FinalStandings = null,
                        ProgressionData = new List<Standings>()
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StandingsCache] Error fetching progression data for {year}: {ex.Message}");
            }

            return new List<Standings>();
        }

        // Get server's last modified timestamp
        private async Task<DateTime?> GetServerLastModifiedAsync(string year)
        {
            try
            {
                var response = await _httpClient.GetFromJsonAsync<DateTime?>($"api/Standings/lastModified/{year}");
                return response;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("timeout") || ex.Message.Contains("canceled"))
            {
                Console.WriteLine($"[StandingsCache] Timeout getting last modified time for {year} - collection likely doesn't exist");
                return DateTime.MinValue; // Indicate data doesn't exist
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StandingsCache] Error getting last modified time: {ex.Message}");
                return null;
            }
        }

        // Manually invalidate cache for a year (useful when standings are updated)
        public void InvalidateCache(string year)
        {
            var cacheKey = $"final_{year}";
            if (_cache.ContainsKey(cacheKey))
            {
                _cache.Remove(cacheKey);
                Console.WriteLine($"[StandingsCache] Manually invalidated cache for {year}");
            }
        }

        // Preload data for performance (optional)
        public async Task WarmCacheAsync(string year)
        {
            try
            {
                // Trigger both final and progression data loading in background
                var finalTask = GetFinalStandingsAsync(year);
                var progressionTask = GetProgressionDataAsync(year);
                
                await Task.WhenAll(finalTask, progressionTask);
                Console.WriteLine($"[StandingsCache] Cache warmed for {year}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StandingsCache] Error warming cache for {year}: {ex.Message}");
            }
        }

        // Get cache status for debugging
        public StandingsCacheStatus GetCacheStatus(string year)
        {
            var cacheKey = $"final_{year}";
            if (_cache.ContainsKey(cacheKey))
            {
                var cached = _cache[cacheKey];
                return new StandingsCacheStatus
                {
                    Year = year,
                    IsCached = true,
                    CachedAt = cached.CachedAt,
                    DataLastUpdated = cached.DataLastUpdated,
                    HasFinalStandings = cached.FinalStandings != null,
                    HasProgressionData = cached.ProgressionData != null,
                    AgeMinutes = (DateTime.UtcNow - cached.CachedAt).TotalMinutes
                };
            }

            return new StandingsCacheStatus
            {
                Year = year,
                IsCached = false
            };
        }
    }

    // Cache data structure
    public class StandingsCache
    {
        public string Year { get; set; }
        public DateTime CachedAt { get; set; }
        public DateTime? DataLastUpdated { get; set; }
        public List<Standings> FinalStandings { get; set; }
        public List<Standings> ProgressionData { get; set; }
    }

    // Cache status for debugging
    public class StandingsCacheStatus
    {
        public string Year { get; set; }
        public bool IsCached { get; set; }
        public DateTime? CachedAt { get; set; }
        public DateTime? DataLastUpdated { get; set; }
        public bool HasFinalStandings { get; set; }
        public bool HasProgressionData { get; set; }
        public double AgeMinutes { get; set; }
    }
}
