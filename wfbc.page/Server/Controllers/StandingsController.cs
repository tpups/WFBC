using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WFBC.Server.DataAccess;
using WFBC.Server.Services;
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
        
        public StandingsController(IStandings _wfbc, ServerSideStandingsCache standingsCache)
        {
            wfbc = _wfbc;
            _standingsCache = standingsCache;
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
                var lastUpdated = await _standingsCache.GetLastUpdatedAsync(year);
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
                // RE-ENABLED: Server-side cache for 90%+ MongoDB query reduction
                var lastUpdated = await _standingsCache.GetLastUpdatedAsync(year);
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

        // Get last updated timestamp for cache validation
        [HttpGet("lastModified/{year}")]
        public async Task<ActionResult<DateTime?>> GetLastModifiedAsync(string year)
        {
            try
            {
                // RE-ENABLED: Server-side cache for 90%+ MongoDB query reduction
                var lastUpdated = await _standingsCache.GetLastUpdatedAsync(year);
                return Ok(lastUpdated);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving last modified time: {ex.Message}");
            }
        }
    }
}
