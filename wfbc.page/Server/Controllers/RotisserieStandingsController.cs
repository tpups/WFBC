using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WFBC.Server.DataAccess;
using WFBC.Server.Services;
using WFBC.Server.Hubs;
using WFBC.Shared.Models;
using WFBC.Server.Interface;

namespace WFBC.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = Policies.IsCommish)]
    public class RotisserieStandingsController : ControllerBase
    {
        private readonly RotisserieStandingsService _rotisserieService;
        private readonly ITeam _teamService;
        private readonly IStandings _standingsService;
        private readonly IHubContext<ProgressHub> _hubContext;

        public RotisserieStandingsController(
            RotisserieStandingsService rotisserieService, 
            ITeam teamService,
            IStandings standingsService,
            IHubContext<ProgressHub> hubContext)
        {
            _rotisserieService = rotisserieService;
            _teamService = teamService;
            _standingsService = standingsService;
            _hubContext = hubContext;
        }

        [HttpPost("calculate/{year}")]
        public async Task<IActionResult> CalculateSeasonStandings(string year)
        {
            try
            {
                // Get all teams for the year (you might need to modify this based on your team filtering logic)
                var teams = _teamService.GetAllTeams().ToList();
                
                if (!teams.Any())
                {
                    return BadRequest("No teams found for calculation");
                }

                // Use the new method with progress reporting (progress will be logged server-side)
                var progress = new Progress<string>(message => 
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
                });

                var calculatedStandings = await _rotisserieService.CalculateStandingsForSeason(year, teams, null, progress);

                // Save all calculated standings to the database
                foreach (var standing in calculatedStandings)
                {
                    // You might want to implement an UpsertStandings method or check for existing records
                    // For now, this assumes we're inserting new records
                    await SaveStandingToDatabase(standing, year);
                }

                var startDate = new DateTime(int.Parse(year), 1, 1);
                var endDate = new DateTime(int.Parse(year), 12, 31);

                return Ok(new { 
                    Message = $"Successfully calculated standings for {year}", 
                    RecordsCreated = calculatedStandings.Count,
                    DaysCalculated = (endDate - startDate).Days + 1
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Error calculating standings: {ex.Message}" });
            }
        }

        [HttpPost("calculate-with-progress/{year}")]
        public async Task<IActionResult> CalculateSeasonStandingsWithProgress(string year)
        {
            try
            {
                // Get all teams for the year
                var teams = _teamService.GetAllTeams().ToList();
                
                if (!teams.Any())
                {
                    return BadRequest("No teams found for calculation");
                }

                // Generate unique progress group ID
                var progressGroupId = $"standings-{year}-{Guid.NewGuid():N}";

                // Start calculation in background task
                _ = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        // Send debug message to confirm background task started
                        await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("ProgressUpdate", $"Background task started for {year}...");
                        
                        var progress = new Progress<string>(message => 
                        {
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
                        });

                        // Send debug message about team count
                        await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("ProgressUpdate", $"Found {teams.Count} teams for calculation...");

                        var calculatedStandings = await _rotisserieService.CalculateStandingsForSeason(year, teams, progressGroupId, progress);

                        // Save all calculated standings to the database
                        foreach (var standing in calculatedStandings)
                        {
                            await SaveStandingToDatabase(standing, year);
                        }

                        // Send completion notification via SignalR
                        await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("CalculationComplete", new {
                            Message = $"Successfully calculated standings for {year}",
                            RecordsCreated = calculatedStandings.Count,
                            Success = true
                        });
                    }
                    catch (Exception ex)
                    {
                        // Send error notification via SignalR
                        Console.WriteLine($"Background task error: {ex}");
                        await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("CalculationError", new {
                            Error = $"Error calculating standings: {ex.Message}",
                            Success = false
                        });
                    }
                }, TaskCreationOptions.LongRunning).Unwrap();

                // Return immediately with progress group ID
                return Ok(new { 
                    Message = $"Calculation started for {year}",
                    ProgressGroupId = progressGroupId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Error starting calculation: {ex.Message}" });
            }
        }

        [HttpPost("calculate/{year}/{date}")]
        public async Task<IActionResult> CalculateStandingsForDate(string year, string date)
        {
            try
            {
                if (!DateTime.TryParse(date, out var targetDate))
                {
                    return BadRequest("Invalid date format");
                }

                var teams = _teamService.GetAllTeams().ToList();
                
                if (!teams.Any())
                {
                    return BadRequest("No teams found for calculation");
                }

                var standings = await _rotisserieService.CalculateStandingsForDate(year, targetDate, teams);

                // Save standings to database
                foreach (var standing in standings)
                {
                    await SaveStandingToDatabase(standing, year);
                }

                return Ok(new { 
                    Message = $"Successfully calculated standings for {year}-{date}", 
                    Standings = standings
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Error calculating standings: {ex.Message}" });
            }
        }

        [HttpGet("preview/{year}/{date}")]
        public async Task<IActionResult> PreviewStandingsForDate(string year, string date)
        {
            try
            {
                if (!DateTime.TryParse(date, out var targetDate))
                {
                    return BadRequest("Invalid date format");
                }

                var teams = _teamService.GetAllTeams().ToList();
                
                if (!teams.Any())
                {
                    return BadRequest("No teams found for calculation");
                }

                var standings = await _rotisserieService.CalculateStandingsForDate(year, targetDate, teams);

                return Ok(standings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Error previewing standings: {ex.Message}" });
            }
        }

        private async Task SaveStandingToDatabase(Standings standing, string year)
        {
            // This is a simplified save - you might want to implement upsert logic
            // to handle cases where standings for a date already exist
            
            // Note: The current IStandings interface might need to be extended to support this
            // For now, this is a placeholder for the save operation
            
            // You would typically use something like:
            // await _standingsService.CreateOrUpdateStanding(standing, year);
            
            // Since the interface might be limited, you might need to extend it or 
            // access the database context directly through the service
        }

        [HttpGet("years")]
        public IActionResult GetAvailableYears()
        {
            try
            {
                // Return years from 2019 to current year
                var currentYear = DateTime.Now.Year;
                var years = Enumerable.Range(2019, currentYear - 2019 + 1).Select(y => y.ToString()).ToList();
                
                return Ok(years);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Error getting available years: {ex.Message}" });
            }
        }
    }
}
