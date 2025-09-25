using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
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
        
        // Static dictionary to track active calculations and their cancellation tokens
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> _activeCalculations = new();

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
                // Get teams for the specific year
                var seasonTeams = _teamService.GetTeamsForSeason(year);
                
                if (!seasonTeams.Any())
                {
                    return BadRequest($"No teams found for {year}");
                }

                // Use the new method with progress reporting (progress will be logged server-side)
                var progress = new Progress<string>(message => 
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
                });

                var calculatedStandings = await _rotisserieService.CalculateStandingsForSeason(year, seasonTeams, null, progress);

                // Save all calculated standings to the database using bulk save
                await SaveStandingsToDatabase(calculatedStandings, year);

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
                // Get teams for the specific year
                var seasonTeams = _teamService.GetTeamsForSeason(year);
                
                if (!seasonTeams.Any())
                {
                    return BadRequest($"No teams found for {year}");
                }

                // Generate unique progress group ID
                var progressGroupId = $"standings-{year}-{Guid.NewGuid():N}";
                
                // Create cancellation token source for this calculation
                var cancellationTokenSource = new CancellationTokenSource();
                _activeCalculations.TryAdd(progressGroupId, cancellationTokenSource);

                // Start calculation in background task
                _ = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        var cancellationToken = cancellationTokenSource.Token;
                        
                        // Send debug message to confirm background task started
                        await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("ProgressUpdate", $"Background task started for {year}...");
                        
                        var progress = new Progress<string>(message => 
                        {
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
                        });

                        // Send debug message about team count
                        await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("ProgressUpdate", $"Found {seasonTeams.Count} teams for calculation...");

                        var calculatedStandings = await _rotisserieService.CalculateStandingsForSeason(year, seasonTeams, progressGroupId, progress, cancellationToken);

                        // Check if cancelled before saving
                        if (cancellationToken.IsCancellationRequested)
                        {
                            await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("CalculationError", new {
                                Error = "Calculation was cancelled",
                                Success = false
                            });
                            return;
                        }

                        // Save all calculated standings to the database using bulk save
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            Console.WriteLine($"[Controller] About to save {calculatedStandings?.Count ?? 0} standings for year {year}");
                            await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("ProgressUpdate", "Saving standings to database...");
                            await SaveStandingsToDatabase(calculatedStandings, year);
                            Console.WriteLine($"[Controller] Finished saving standings for year {year}");
                        }

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            // Send completion notification via SignalR
                            await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("CalculationComplete", new {
                                Message = $"Successfully calculated standings for {year}",
                                RecordsCreated = calculatedStandings.Count,
                                Success = true
                            });
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancelled (catches both OperationCanceledException and TaskCanceledException)
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Calculation for {year} was cancelled");
                        await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("CalculationError", new {
                            Error = "Calculation was cancelled",
                            Success = false
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
                    finally
                    {
                        // Clean up the cancellation token
                        _activeCalculations.TryRemove(progressGroupId, out _);
                        cancellationTokenSource?.Dispose();
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

        [HttpPost("cancel/{progressGroupId}")]
        public IActionResult CancelCalculation(string progressGroupId)
        {
            try
            {
                if (_activeCalculations.TryGetValue(progressGroupId, out var cancellationTokenSource))
                {
                    cancellationTokenSource.Cancel();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Cancellation requested for {progressGroupId}");
                    return Ok(new { Message = "Cancellation requested", Success = true });
                }
                else
                {
                    return NotFound(new { Message = "Calculation not found or already completed", Success = false });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Error cancelling calculation: {ex.Message}" });
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

                var seasonTeams = _teamService.GetTeamsForSeason(year);
                
                if (!seasonTeams.Any())
                {
                    return BadRequest($"No teams found for {year}");
                }

                var standings = await _rotisserieService.CalculateStandingsForDate(year, targetDate, seasonTeams);

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

                var seasonTeams = _teamService.GetTeamsForSeason(year);
                
                if (!seasonTeams.Any())
                {
                    return BadRequest($"No teams found for {year}");
                }

                var standings = await _rotisserieService.CalculateStandingsForDate(year, targetDate, seasonTeams);

                return Ok(standings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Error previewing standings: {ex.Message}" });
            }
        }

        private async Task SaveStandingToDatabase(Standings standing, string year)
        {
            // Save individual standing record - used for single date calculations
            await _standingsService.SaveStandingsAsync(new List<Standings> { standing }, year);
        }

        private async Task SaveStandingsToDatabase(List<Standings> standings, string year)
        {
            // Save multiple standings records - used for bulk season calculations
            await _standingsService.SaveStandingsAsync(standings, year);
        }

        [HttpGet("check/{year}")]
        public async Task<IActionResult> CheckExistingStandings(string year)
        {
            try
            {
                var standingsInfo = await _standingsService.GetExistingStandingsInfoAsync(year);
                
                return Ok(new
                {
                    Year = year,
                    Exist = standingsInfo.Exist,
                    LastUpdated = standingsInfo.LastUpdated,
                    RecordCount = standingsInfo.RecordCount
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Error checking existing standings: {ex.Message}" });
            }
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
