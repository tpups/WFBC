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

        // Get final standings with robust caching
        public async Task<List<Standings>> GetFinalStandingsAsync(string year)
        {
            var cacheKey = $"final_standings_{year}"; // Isolated cache key
            
            // Check for valid cached data
            if (_cache.ContainsKey(cacheKey))
            {
                var cached = _cache[cacheKey];
                var cacheAge = DateTime.UtcNow - cached.CachedAt;
                
                // Simple 5-minute expiration - no complex validation that can fail
                if (cacheAge < TimeSpan.FromMinutes(5) && cached.FinalStandings?.Any() == true)
                {
                    Console.WriteLine($"[StandingsCache] Serving {cached.FinalStandings.Count} final standings for {year} from cache ({cacheAge.TotalMinutes:F1}min old)");
                    return cached.FinalStandings;
                }
                else
                {
                    Console.WriteLine($"[StandingsCache] Cache expired or empty for {year} - removing and fetching fresh");
                    _cache.Remove(cacheKey);
                }
            }

            // Fetch fresh data from server
            Console.WriteLine($"[StandingsCache] Fetching fresh final standings for {year}");
            try
            {
                var response = await _httpClient.GetFromJsonAsync<StandingsResponse<List<Standings>>>($"api/Standings/final/{year}");
                
                if (response?.Data?.Any() == true)
                {
                    Console.WriteLine($"[StandingsCache] Server returned {response.Data.Count} final standings for {year} - caching for 5 minutes");
                    
                    // ONLY cache successful responses with actual data
                    _cache[cacheKey] = new StandingsCache
                    {
                        Year = year,
                        CachedAt = DateTime.UtcNow,
                        DataLastUpdated = response.LastUpdated ?? DateTime.UtcNow,
                        FinalStandings = response.Data,
                        ProgressionData = null // Not applicable for this cache entry
                    };

                    return response.Data;
                }
                else
                {
                    Console.WriteLine($"[StandingsCache] Server returned empty/null data for {year} - NOT caching empty result");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StandingsCache] Error fetching final standings for {year}: {ex.Message}");
            }

            // Return empty list but DON'T cache it
            Console.WriteLine($"[StandingsCache] Returning empty list for {year} (not cached)");
            return new List<Standings>();
        }

        // Get progression data with robust caching
        public async Task<List<Standings>> GetProgressionDataAsync(string year)
        {
            var cacheKey = $"progression_data_{year}"; // Isolated cache key
            
            // Check for valid cached data
            if (_cache.ContainsKey(cacheKey))
            {
                var cached = _cache[cacheKey];
                var cacheAge = DateTime.UtcNow - cached.CachedAt;
                
                // Simple 5-minute expiration - no complex validation that can fail
                if (cacheAge < TimeSpan.FromMinutes(5) && cached.ProgressionData?.Any() == true)
                {
                    Console.WriteLine($"[StandingsCache] Serving {cached.ProgressionData.Count} progression data records for {year} from cache ({cacheAge.TotalMinutes:F1}min old)");
                    return cached.ProgressionData;
                }
                else
                {
                    Console.WriteLine($"[StandingsCache] Progression cache expired or empty for {year} - removing and fetching fresh");
                    _cache.Remove(cacheKey);
                }
            }

            // Fetch fresh data from server
            Console.WriteLine($"[StandingsCache] Fetching fresh progression data for {year}");
            try
            {
                var response = await _httpClient.GetFromJsonAsync<StandingsResponse<List<Standings>>>($"api/Standings/progression/{year}");
                
                if (response?.Data?.Any() == true)
                {
                    Console.WriteLine($"[StandingsCache] Server returned {response.Data.Count} progression data records for {year} - caching for 5 minutes");
                    
                    // ONLY cache successful responses with actual data
                    _cache[cacheKey] = new StandingsCache
                    {
                        Year = year,
                        CachedAt = DateTime.UtcNow,
                        DataLastUpdated = response.LastUpdated ?? DateTime.UtcNow,
                        FinalStandings = null, // Not applicable for this cache entry
                        ProgressionData = response.Data
                    };

                    return response.Data;
                }
                else
                {
                    Console.WriteLine($"[StandingsCache] Server returned empty/null progression data for {year} - NOT caching empty result");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StandingsCache] Error fetching progression data for {year}: {ex.Message}");
            }

            // Return empty list but DON'T cache it
            Console.WriteLine($"[StandingsCache] Returning empty list for progression data for {year} (not cached)");
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
            var finalCacheKey = $"final_standings_{year}";
            var progressionCacheKey = $"progression_data_{year}";
            
            bool removedAny = false;
            
            if (_cache.ContainsKey(finalCacheKey))
            {
                _cache.Remove(finalCacheKey);
                removedAny = true;
            }
            
            if (_cache.ContainsKey(progressionCacheKey))
            {
                _cache.Remove(progressionCacheKey);
                removedAny = true;
            }
            
            if (removedAny)
            {
                Console.WriteLine($"[StandingsCache] Manually invalidated all cache for {year}");
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
            var finalCacheKey = $"final_standings_{year}";
            var progressionCacheKey = $"progression_data_{year}";
            
            var finalCached = _cache.ContainsKey(finalCacheKey) ? _cache[finalCacheKey] : null;
            var progressionCached = _cache.ContainsKey(progressionCacheKey) ? _cache[progressionCacheKey] : null;
            
            // Return status if either cache exists
            if (finalCached != null || progressionCached != null)
            {
                var mostRecentCache = finalCached ?? progressionCached;
                return new StandingsCacheStatus
                {
                    Year = year,
                    IsCached = true,
                    CachedAt = mostRecentCache.CachedAt,
                    DataLastUpdated = mostRecentCache.DataLastUpdated,
                    HasFinalStandings = finalCached?.FinalStandings != null,
                    HasProgressionData = progressionCached?.ProgressionData != null,
                    AgeMinutes = (DateTime.UtcNow - mostRecentCache.CachedAt).TotalMinutes
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
