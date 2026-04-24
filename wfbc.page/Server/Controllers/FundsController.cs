using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using WFBC.Server.Models;
using WFBC.Shared.Models;

namespace WFBC.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FundsController : ControllerBase
    {
        private readonly WfbcDBContext _db;

        public FundsController(WfbcDBContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Get all funds data points for a year, plus the season start date
        /// </summary>
        [HttpGet("{year}")]
        public async Task<ActionResult<FundsResponse>> GetFundsData(string year)
        {
            try
            {
                var collection = _db.TheFunds[year];
                var dataPoints = await collection
                    .Find(FilterDefinition<FundsDataPoint>.Empty)
                    .SortBy(d => d.Date)
                    .ToListAsync();

                // Get season start date
                var settings = await _db.Settings
                    .Find(Builders<SeasonSettings>.Filter.Eq("year", int.Parse(year)))
                    .FirstOrDefaultAsync();

                var response = new FundsResponse
                {
                    DataPoints = dataPoints,
                    SeasonStartDate = settings?.SeasonStartDate ?? new DateTime(int.Parse(year), 3, 1)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving funds data: {ex.Message}");
            }
        }

        /// <summary>
        /// Add a new funds data point
        /// </summary>
        [HttpPost("{year}")]
        [Authorize(Policy = Policies.IsCommish)]
        public async Task<ActionResult> AddFundsDataPoint(string year, [FromBody] FundsDataPoint dataPoint)
        {
            try
            {
                dataPoint.Id = null; // Let MongoDB generate the ID
                dataPoint.CreatedAt = DateTime.UtcNow;

                var collection = _db.TheFunds[year];
                await collection.InsertOneAsync(dataPoint);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error adding funds data point: {ex.Message}");
            }
        }

        /// <summary>
        /// Delete a funds data point
        /// </summary>
        [HttpDelete("{year}/{id}")]
        [Authorize(Policy = Policies.IsCommish)]
        public async Task<ActionResult> DeleteFundsDataPoint(string year, string id)
        {
            try
            {
                var collection = _db.TheFunds[year];
                var filter = Builders<FundsDataPoint>.Filter.Eq("_id", new ObjectId(id));
                await collection.DeleteOneAsync(filter);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting funds data point: {ex.Message}");
            }
        }
    }
}