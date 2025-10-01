using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using WFBC.Shared.Models;
using WFBC.Server.Interface;

namespace WFBC.Server.Services
{
    public class ServerSideStandingsCache
    {
        private readonly IMemoryCache _cache;
        private readonly IStandings _standingsDataAccess;
        private readonly ILogger<ServerSideStandingsCache> _logger;

        // Cache keys
        private const string FINAL_STANDINGS_KEY_PREFIX = "final_standings_";
        private const string PROGRESSION_DATA_KEY_PREFIX = "progression_data_";
        private const string LAST_UPDATED_KEY_PREFIX = "last_updated_";

        public ServerSideStandingsCache(
            IMemoryCache cache, 
            IStandings standingsDataAccess,
            ILogger<ServerSideStandingsCache> logger)
        {
            _cache = cache;
            _standingsDataAccess = standingsDataAccess;
            _logger = logger;
        }

        /// <summary>
        /// Get final standings for a year with server-side caching
        /// </summary>
        public async Task<List<Standings>> GetFinalStandingsAsync(string year)
        {
            var cacheKey = $"{FINAL_STANDINGS_KEY_PREFIX}{year}";
            
            if (_cache.TryGetValue(cacheKey, out List<Standings>? cachedStandings))
            {
                _logger.LogInformation($"[ServerCache] Serving final standings for {year} from server cache ({cachedStandings?.Count ?? 0} records)");
                return cachedStandings ?? new List<Standings>();
            }

            _logger.LogInformation($"[ServerCache] Cache miss for final standings {year} - fetching from database");
            
            // Fetch from database
            var standings = await _standingsDataAccess.GetFinalStandingsForYearAsync(year);
            
            if (standings?.Any() == true)
            {
                // Cache indefinitely - will be explicitly invalidated when data changes
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    Priority = CacheItemPriority.Normal,
                    SlidingExpiration = null, // No sliding expiration
                    AbsoluteExpiration = null // No absolute expiration - cache indefinitely
                };

                _cache.Set(cacheKey, standings, cacheOptions);
                _logger.LogInformation($"[ServerCache] Cached final standings for {year} ({standings.Count} records) - will persist until explicitly invalidated");
            }
            else
            {
                _logger.LogInformation($"[ServerCache] No final standings found for {year} - not caching empty result");
            }

            return standings ?? new List<Standings>();
        }

        /// <summary>
        /// Get progression data for a year with server-side caching
        /// </summary>
        public async Task<List<Standings>> GetProgressionDataAsync(string year)
        {
            var cacheKey = $"{PROGRESSION_DATA_KEY_PREFIX}{year}";
            
            if (_cache.TryGetValue(cacheKey, out List<Standings>? cachedProgression))
            {
                _logger.LogInformation($"[ServerCache] Serving progression data for {year} from server cache ({cachedProgression?.Count ?? 0} records)");
                return cachedProgression ?? new List<Standings>();
            }

            _logger.LogInformation($"[ServerCache] Cache miss for progression data {year} - fetching from database");
            
            // Fetch from database
            var progressionData = await _standingsDataAccess.GetStandingsProgressionForYearAsync(year);
            
            if (progressionData?.Any() == true)
            {
                // Cache indefinitely - will be explicitly invalidated when data changes
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    Priority = CacheItemPriority.Normal,
                    SlidingExpiration = null, // No sliding expiration
                    AbsoluteExpiration = null // No absolute expiration - cache indefinitely
                };

                _cache.Set(cacheKey, progressionData, cacheOptions);
                _logger.LogInformation($"[ServerCache] Cached progression data for {year} ({progressionData.Count} records) - will persist until explicitly invalidated");
            }
            else
            {
                _logger.LogInformation($"[ServerCache] No progression data found for {year} - not caching empty result");
            }

            return progressionData ?? new List<Standings>();
        }

        /// <summary>
        /// Get last updated timestamp for a year with server-side caching
        /// </summary>
        public async Task<DateTime?> GetLastUpdatedAsync(string year)
        {
            var cacheKey = $"{LAST_UPDATED_KEY_PREFIX}{year}";
            
            if (_cache.TryGetValue(cacheKey, out DateTime? cachedTimestamp))
            {
                _logger.LogInformation($"[ServerCache] Serving last updated timestamp for {year} from server cache: {cachedTimestamp}");
                return cachedTimestamp;
            }

            _logger.LogInformation($"[ServerCache] Cache miss for last updated {year} - fetching from database");
            
            // Fetch from database
            var lastUpdated = await _standingsDataAccess.GetStandingsLastUpdatedAsync(year);
            
            if (lastUpdated.HasValue)
            {
                // Cache indefinitely - will be explicitly invalidated when data changes
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    Priority = CacheItemPriority.Normal,
                    SlidingExpiration = null, // No sliding expiration
                    AbsoluteExpiration = null // No absolute expiration - cache indefinitely
                };

                _cache.Set(cacheKey, lastUpdated, cacheOptions);
                _logger.LogInformation($"[ServerCache] Cached last updated timestamp for {year}: {lastUpdated} - will persist until explicitly invalidated");
            }
            else
            {
                _logger.LogInformation($"[ServerCache] No last updated timestamp found for {year} - not caching null result");
            }

            return lastUpdated;
        }

        /// <summary>
        /// Explicitly invalidate all cached data for a specific year
        /// Called by RotisserieStandingsService after standings calculation
        /// </summary>
        public void InvalidateYearCache(string year)
        {
            var finalKey = $"{FINAL_STANDINGS_KEY_PREFIX}{year}";
            var progressionKey = $"{PROGRESSION_DATA_KEY_PREFIX}{year}";
            var lastUpdatedKey = $"{LAST_UPDATED_KEY_PREFIX}{year}";

            _cache.Remove(finalKey);
            _cache.Remove(progressionKey);
            _cache.Remove(lastUpdatedKey);

            _logger.LogInformation($"[ServerCache] Explicitly invalidated all cached data for year {year} (final, progression, last updated)");
        }

        /// <summary>
        /// Get cache status for debugging/monitoring
        /// </summary>
        public CacheStatus GetCacheStatus(string year)
        {
            var finalKey = $"{FINAL_STANDINGS_KEY_PREFIX}{year}";
            var progressionKey = $"{PROGRESSION_DATA_KEY_PREFIX}{year}";
            var lastUpdatedKey = $"{LAST_UPDATED_KEY_PREFIX}{year}";

            var hasFinal = _cache.TryGetValue(finalKey, out _);
            var hasProgression = _cache.TryGetValue(progressionKey, out _);
            var hasLastUpdated = _cache.TryGetValue(lastUpdatedKey, out _);

            return new CacheStatus
            {
                Year = year,
                HasFinalStandings = hasFinal,
                HasProgressionData = hasProgression,
                HasLastUpdated = hasLastUpdated,
                CacheType = "ServerSide"
            };
        }

        /// <summary>
        /// Warm cache for a specific year (optional performance optimization)
        /// </summary>
        public async Task WarmCacheAsync(string year)
        {
            _logger.LogInformation($"[ServerCache] Warming cache for year {year}");
            
            try
            {
                // Trigger all data loading in parallel
                var finalTask = GetFinalStandingsAsync(year);
                var progressionTask = GetProgressionDataAsync(year);
                var lastUpdatedTask = GetLastUpdatedAsync(year);
                
                await Task.WhenAll(finalTask, progressionTask, lastUpdatedTask);
                
                _logger.LogInformation($"[ServerCache] Successfully warmed cache for year {year}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[ServerCache] Error warming cache for year {year}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Cache status information for debugging
    /// </summary>
    public class CacheStatus
    {
        public string Year { get; set; } = string.Empty;
        public bool HasFinalStandings { get; set; }
        public bool HasProgressionData { get; set; }
        public bool HasLastUpdated { get; set; }
        public string CacheType { get; set; } = string.Empty;
    }
}
