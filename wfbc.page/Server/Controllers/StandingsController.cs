using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using WFBC.Server.DataAccess;
using WFBC.Server.Services;
using WFBC.Server.Models;
using WFBC.Shared.Models;
using WFBC.Server.Interface;

namespace WFBC.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StandingsController : ControllerBase
    {
        private readonly IStandings wfbc;
        private readonly ServerSideStandingsCache _standingsCache;
        private readonly WfbcDBContext _db;
        
        public StandingsController(IStandings _wfbc, ServerSideStandingsCache standingsCache, WfbcDBContext db)
        {
            wfbc = _wfbc;
            _standingsCache = standingsCache;
            _db = db;
        }

        [HttpGet]
        public Standings StandingsByDate(string date)
        {
            DateTime _date = DateTime.Parse(date);
            return wfbc.GetStandingsByDate(_date);
        }

        // Get final standings for a year with cache headers
        [HttpGet("final/{year}")]
        public async Task<ActionResult<StandingsResponse<List<Standings>>>> GetFinalStandingsAsync(string year)
        {
            try
            {
                // RE-ENABLED: Server-side cache for 90%+ MongoDB query reduction
                // Use box score download timestamp instead of standings calculation timestamp
                var lastBoxScoreUpdate = await _standingsCache.GetLastBoxScoreUpdateAsync(year);
                var lastUpdated = lastBoxScoreUpdate ?? await _standingsCache.GetLastUpdatedAsync(year);
                var standings = await _standingsCache.GetFinalStandingsAsync(year);

                var response = new StandingsResponse<List<Standings>>
                {
                    Data = standings,
                    LastUpdated = lastUpdated,
                    CacheKey = lastUpdated?.ToString("yyyyMMddHHmmss") ?? "no-data"
                };

                // Set cache headers
                if (lastUpdated.HasValue)
                {
                    Response.Headers.Add("Last-Modified", lastUpdated.Value.ToString("R"));
                    Response.Headers.Add("Cache-Control", "public, max-age=300"); // 5 minute cache
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving final standings: {ex.Message}");
            }
        }

        // Get progression data for a year with cache headers
        [HttpGet("progression/{year}")]
        public async Task<ActionResult<StandingsResponse<List<Standings>>>> GetProgressionDataAsync(string year)
        {
            try
            {
                // Use box score download timestamp instead of standings calculation timestamp
                var lastBoxScoreUpdate = await _standingsCache.GetLastBoxScoreUpdateAsync(year);
                var lastUpdated = lastBoxScoreUpdate ?? await _standingsCache.GetLastUpdatedAsync(year);
                var standings = await _standingsCache.GetProgressionDataAsync(year);

                var response = new StandingsResponse<List<Standings>>
                {
                    Data = standings,
                    LastUpdated = lastUpdated,
                    CacheKey = lastUpdated?.ToString("yyyyMMddHHmmss") ?? "no-data"
                };

                // Set cache headers
                if (lastUpdated.HasValue)
                {
                    Response.Headers.Add("Last-Modified", lastUpdated.Value.ToString("R"));
                    Response.Headers.Add("Cache-Control", "public, max-age=300"); // 5 minute cache
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving progression data: {ex.Message}");
            }
        }

        // Get wager data for a year (returns wagers + last box score update timestamp)
        [HttpGet("wagers/{year}")]
        public async Task<ActionResult<StandingsResponse<List<WagerData>>>> GetWagerDataAsync(string year)
        {
            try
            {
                var collection = _db.CompiledWagerData[year];
                var filter = Builders<CompiledWagerData>.Filter.And(
                    Builders<CompiledWagerData>.Filter.Eq("year", year),
                    Builders<CompiledWagerData>.Filter.Eq("type", "wager_data")
                );

                var compiled = await collection.Find(filter).FirstOrDefaultAsync();

                var response = new StandingsResponse<List<WagerData>>
                {
                    Data = compiled?.Wagers ?? new List<WagerData>(),
                    LastUpdated = compiled?.LastBoxScoreUpdate,
                    CacheKey = compiled?.LastBoxScoreUpdate?.ToString("yyyyMMddHHmmss") ?? "no-data"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving wager data: {ex.Message}");
            }
        }

        // Get last updated timestamp for cache validation
        [HttpGet("lastModified/{year}")]
        public async Task<ActionResult<DateTime?>> GetLastModifiedAsync(string year)
        {
            try
            {
                // Use box score update timestamp (same as data endpoints) for consistent cache validation
                var lastBoxScoreUpdate = await _standingsCache.GetLastBoxScoreUpdateAsync(year);
                var lastUpdated = lastBoxScoreUpdate ?? await _standingsCache.GetLastUpdatedAsync(year);
                return Ok(lastUpdated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving last modified time: {ex.Message}");
            }
        }
    }
}
