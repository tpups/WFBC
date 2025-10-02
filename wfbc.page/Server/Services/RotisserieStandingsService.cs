using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using WFBC.Shared.Models;
using WFBC.Server.Models;
using WFBC.Server.Interface;
using Microsoft.AspNetCore.SignalR;
using WFBC.Server.Hubs;

namespace WFBC.Server.Services
{
    public class RotisserieStandingsService
    {
        private readonly WfbcDBContext _db;
        private readonly IHubContext<ProgressHub> _hubContext;
        private readonly ISeasonSettings _seasonSettings;
        private readonly ServerSideStandingsCache _standingsCache;

        public RotisserieStandingsService(WfbcDBContext db, IHubContext<ProgressHub> hubContext, ISeasonSettings seasonSettings, ServerSideStandingsCache standingsCache)
        {
            _db = db;
            _hubContext = hubContext;
            _seasonSettings = seasonSettings;
            _standingsCache = standingsCache;
        }

        public async Task<List<Standings>> CalculateStandingsForDate(string year, DateTime date, List<SeasonTeam> teams)
        {
            var startDate = new DateTime(int.Parse(year), 1, 1);
            var endDate = date;

            var teamTotals = await GetTeamTotals(year, teams, startDate, endDate);
            
            if (teamTotals == null) return new List<Standings>();

            var hittingCategories = new[] { "AVG", "OPS", "R", "SB", "HR", "RBI" };
            var pitchingCategories = year == "2019" 
                ? new[] { "ERA", "WHIP", "IP", "K", "S", "QS" }
                : new[] { "ERA", "WHIP", "IP", "K", "SV", "QS" };

            var hittingPoints = CalculateHittingPoints(teamTotals, hittingCategories, teams.Count);
            var pitchingPoints = CalculatePitchingPoints(teamTotals, pitchingCategories, teams.Count);

            var standings = BuildStandingsFromPoints(hittingPoints, pitchingPoints, teams, year, date);

            return standings;
        }

        public async Task<List<Standings>> CalculateStandingsForSeason(string year, List<SeasonTeam> teams, string? progressGroupId = null, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            // Get season settings for the specified year
            var seasonSettings = _seasonSettings.GetSeasonSettings(int.Parse(year));
            
            // If no settings exist, create and save default ones
            if (seasonSettings == null)
            {
                seasonSettings = new SeasonSettings(int.Parse(year));
                _seasonSettings.AddSeasonSettings(seasonSettings);
            }
            
            var startDate = seasonSettings.SeasonStartDate;
            var endDate = seasonSettings.SeasonEndDate;
            var allStandings = new List<Standings>();

            var startMessage = $"Starting optimized calculation for {year} season from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}...";
            if (!string.IsNullOrEmpty(progressGroupId))
            {
                await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("ProgressUpdate", startMessage);
            }
            progress?.Report(startMessage);

            // PERFORMANCE OPTIMIZATION: Load all season data once instead of querying for each day
            var loadingMessage = "Loading all season box score data...";
            if (!string.IsNullOrEmpty(progressGroupId))
            {
                await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("ProgressUpdate", loadingMessage);
            }
            progress?.Report(loadingMessage);

            var seasonBoxScores = await LoadAllSeasonBoxScores(year, startDate, endDate);
            if (seasonBoxScores == null)
            {
                throw new Exception("Failed to load season box score data");
            }

            cancellationToken.ThrowIfCancellationRequested();

            var processingMessage = "Processing daily standings with loaded data...";
            if (!string.IsNullOrEmpty(progressGroupId))
            {
                await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("ProgressUpdate", processingMessage);
            }
            progress?.Report(processingMessage);

            // Process daily standings incrementally using pre-loaded data
            allStandings = await ProcessIncrementalStandings(year, teams, startDate, endDate, seasonBoxScores, progressGroupId, progress, cancellationToken);

            var completedMessage = $"Completed optimized calculation for {year} season! Processed {(endDate - startDate).Days + 1} days from {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}.";
            if (!string.IsNullOrEmpty(progressGroupId))
            {
                await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("ProgressUpdate", completedMessage);
            }
            progress?.Report(completedMessage);
            
            // CRITICAL: Invalidate server-side cache after standings calculation
            // This ensures fresh data is served on the next request
            _standingsCache.InvalidateYearCache(year);
            
            // Generate compiled documents for optimized performance
            var compilationMessage = "Generating compiled standings documents for optimized performance...";
            if (!string.IsNullOrEmpty(progressGroupId))
            {
                await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("ProgressUpdate", compilationMessage);
            }
            progress?.Report(compilationMessage);
            
            await GenerateCompiledDocumentsAsync(year, allStandings, cancellationToken);
            
            return allStandings;
        }

        private async Task<Dictionary<string, Dictionary<string, Dictionary<string, object>>>> GetTeamTotals(
            string year, List<SeasonTeam> teams, DateTime startDate, DateTime endDate)
        {
            try
            {
                var totals = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();
                
                foreach (var team in teams)
                {
                    totals[team.TeamId!] = new Dictionary<string, Dictionary<string, object>>
                    {
                        ["hit"] = new Dictionary<string, object>(),
                        ["pitch"] = new Dictionary<string, object>()
                    };
                }

                // Get hitting box scores - filter for team totals like Python script
                var hitCollection = _db.BoxScores[year]["hitting"];
                var hitFilter = Builders<Box>.Filter.And(
                    Builders<Box>.Filter.Eq("player", "TOT"),
                    Builders<Box>.Filter.Eq("position", "A"),
                    Builders<Box>.Filter.Gte("stats_date", startDate.ToString("yyyy-MM-dd")),
                    Builders<Box>.Filter.Lte("stats_date", endDate.ToString("yyyy-MM-dd"))
                );
                var hitBoxScores = await hitCollection.Find(hitFilter).ToListAsync();

                // Get pitching box scores - filter for team totals like Python script
                var pitchCollection = _db.BoxScores[year]["pitching"];
                var pitchFilter = Builders<Box>.Filter.And(
                    Builders<Box>.Filter.Eq("player", "TOT"),
                    Builders<Box>.Filter.Eq("position", "A"),
                    Builders<Box>.Filter.Gte("stats_date", startDate.ToString("yyyy-MM-dd")),
                    Builders<Box>.Filter.Lte("stats_date", endDate.ToString("yyyy-MM-dd"))
                );
                var pitchBoxScores = await pitchCollection.Find(pitchFilter).ToListAsync();

                // Aggregate hitting stats - using same field names as Python script
                var hStats = new[] { "2B", "3B", "AB", "BB", "H", "HBP", "HR", "PA", "R", "RBI", "SB", "SF" };
                
                foreach (var score in hitBoxScores)
                {
                    if (score.TeamId != null && totals.ContainsKey(score.TeamId))
                    {
                        var team = totals[score.TeamId]["hit"];
                        
                        foreach (var stat in hStats)
                        {
                            var value = GetHittingStatValue(score, stat);
                            if (value.HasValue)
                            {
                                if (team.ContainsKey(stat))
                                {
                                    team[stat] = (int)team[stat] + value.Value;
                                }
                                else
                                {
                                    team[stat] = value.Value;
                                }
                            }
                        }
                    }
                }

                // Aggregate pitching stats - using same field names as Python script
                var pStats = year == "2019" 
                    ? new[] { "BB", "ER", "H", "HB", "IP", "K", "QS", "S" }
                    : new[] { "BB", "ER", "H", "HB", "IP", "K", "QS", "SV" };

                foreach (var score in pitchBoxScores)
                {
                    if (score.TeamId != null && totals.ContainsKey(score.TeamId))
                    {
                        var team = totals[score.TeamId]["pitch"];
                        
                        foreach (var stat in pStats)
                        {
                            if (stat == "IP")
                            {
                                // Handle IP special case like Python script
                                if (!string.IsNullOrEmpty(score.InningsPitched))
                                {
                                    decimal innings = decimal.Parse(score.InningsPitched);
                                    
                                    // Handle 2019 special IP calculation
                                    if (year == "2019")
                                    {
                                        decimal partialInnings = innings % 1 * 3.3m;
                                        decimal fullInnings = Math.Floor(innings);
                                        innings = fullInnings + partialInnings;
                                    }

                                    if (team.ContainsKey(stat))
                                    {
                                        decimal currentTotal = (decimal)team[stat];
                                        decimal newTotal = currentTotal + innings;
                                        decimal outs = Math.Round(newTotal * 3);
                                        decimal newIP = Math.Round(outs / 3, 2);
                                        team[stat] = newIP;
                                    }
                                    else
                                    {
                                        team[stat] = innings;
                                    }
                                }
                            }
                            else
                            {
                                var value = GetPitchingStatValue(score, stat);
                                if (value.HasValue)
                                {
                                    if (team.ContainsKey(stat))
                                    {
                                        team[stat] = (int)team[stat] + value.Value;
                                    }
                                    else
                                    {
                                        team[stat] = value.Value;
                                    }
                                }
                            }
                        }
                    }
                }

                // Calculate Quality Appearances like Python script
                await CalculateQualityAppearances(year, startDate, endDate, totals);

                return totals;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetTeamTotals: {ex.Message}");
                return null;
            }
        }

        private async Task CalculateQualityAppearances(string year, DateTime startDate, DateTime endDate, 
            Dictionary<string, Dictionary<string, Dictionary<string, object>>> totals)
        {
            var pitchCollection = _db.BoxScores[year]["pitching"];
            var qaFilter = Builders<Box>.Filter.And(
                Builders<Box>.Filter.Eq("position", "P"),
                Builders<Box>.Filter.Ne("player", "TOT"),
                Builders<Box>.Filter.Gte("stats_date", startDate.ToString("yyyy-MM-dd")),
                Builders<Box>.Filter.Lte("stats_date", endDate.ToString("yyyy-MM-dd"))
            );
            var pitchBoxScores = await pitchCollection.Find(qaFilter).ToListAsync();

            foreach (var score in pitchBoxScores)
            {
                if (score.TeamId != null && totals.ContainsKey(score.TeamId))
                {
                    if (!string.IsNullOrEmpty(score.InningsPitched) && score.EarnedRuns != null)
                    {
                        decimal ip = decimal.Parse(score.InningsPitched);
                        var er = ConvertToInt(score.EarnedRuns);
                        
                        if (ip >= 5 && er.HasValue)
                        {
                            decimal era = ip > 0 ? (9 * er.Value) / ip : 99999;
                            if (era <= 4.5m)
                            {
                                var team = totals[score.TeamId]["pitch"];
                                if (team.ContainsKey("QA"))
                                {
                                    team["QA"] = (int)team["QA"] + 1;
                                }
                                else
                                {
                                    team["QA"] = 1;
                                }
                            }
                        }
                    }
                }
            }
        }

        private int? GetHittingStatValue(Box box, string stat)
        {
            var objectValue = stat switch
            {
                "2B" => box.Doubles,
                "3B" => box.Triples,
                "AB" => box.AtBats,
                "BB" => box.Walks,
                "H" => box.Hits,
                "HBP" => box.HitByPitch,
                "HR" => box.HomeRuns,
                "PA" => box.PlateAppearances,
                "R" => box.Runs,
                "RBI" => box.RunsBattedIn,
                "SB" => box.StolenBases,
                "SF" => box.SacrificeFlies,
                _ => null
            };

            return ConvertToInt(objectValue);
        }

        private int? GetPitchingStatValue(Box box, string stat)
        {
            var objectValue = stat switch
            {
                "BB" => box.Walks,
                "ER" => box.EarnedRuns,
                "H" => box.Hits,
                "HB" => box.HitBatters,
                "K" => box.Strikeouts,
                "QS" => box.QualityStarts,
                "S" => box.SavesAlternate, // For 2019
                "SV" => box.Saves, // For 2020+
                _ => null
            };

            return ConvertToInt(objectValue);
        }

        private int? ConvertToInt(object? value)
        {
            if (value == null) return null;
            
            // Handle direct int values
            if (value is int intVal) return intVal;
            
            // Handle string values
            if (value is string stringVal && !string.IsNullOrEmpty(stringVal))
            {
                if (int.TryParse(stringVal, out var parsed))
                    return parsed;
            }
            
            return null;
        }

        private Dictionary<string, List<TeamCategoryResult>> CalculateHittingPoints(
            Dictionary<string, Dictionary<string, Dictionary<string, object>>> teamTotals,
            string[] categories, int teamCount)
        {
            var points = Enumerable.Range(1, teamCount).Reverse().ToArray();
            var hittingPoints = new Dictionary<string, List<TeamCategoryResult>>();

            foreach (var cat in categories)
            {
                var categoryResults = new List<TeamCategoryResult>();

                foreach (var teamId in teamTotals.Keys)
                {
                    var team = teamTotals[teamId]["hit"];
                    decimal statValue = 0;

                    if (cat == "AVG")
                    {
                        int hits = team.ContainsKey("H") ? (int)team["H"] : 0;
                        int atBats = team.ContainsKey("AB") ? (int)team["AB"] : 0;
                        statValue = atBats > 0 ? (decimal)hits / atBats : 0;
                    }
                    else if (cat == "OPS")
                    {
                        int ab = team.ContainsKey("AB") ? (int)team["AB"] : 0;
                        int h = team.ContainsKey("H") ? (int)team["H"] : 0;
                        int bb = team.ContainsKey("BB") ? (int)team["BB"] : 0;
                        int hbp = team.ContainsKey("HBP") ? (int)team["HBP"] : 0;
                        int sf = team.ContainsKey("SF") ? (int)team["SF"] : 0;
                        int doubles = team.ContainsKey("2B") ? (int)team["2B"] : 0;
                        int triples = team.ContainsKey("3B") ? (int)team["3B"] : 0;
                        int hr = team.ContainsKey("HR") ? (int)team["HR"] : 0;

                        decimal obp = (ab + bb + sf + hbp) > 0 ? (decimal)(h + bb + hbp) / (ab + bb + sf + hbp) : 0;
                        int singles = h - doubles - triples - hr;
                        int totalBases = singles + (2 * doubles) + (3 * triples) + (4 * hr);
                        decimal slg = ab > 0 ? (decimal)totalBases / ab : 0;
                        statValue = obp + slg;
                    }
                    else
                    {
                        var mappedStat = MapCategoryToStat(cat);
                        statValue = team.ContainsKey(mappedStat) ? (int)team[mappedStat] : 0;
                    }

                    categoryResults.Add(new TeamCategoryResult
                    {
                        TeamId = teamId,
                        Value = statValue
                    });
                }

                // Sort and assign points
                var sortedResults = categoryResults.OrderByDescending(x => x.Value).ToList();
                AssignPointsWithTies(sortedResults, points);
                hittingPoints[cat] = sortedResults;
            }

            return hittingPoints;
        }

        private Dictionary<string, List<TeamCategoryResult>> CalculatePitchingPoints(
            Dictionary<string, Dictionary<string, Dictionary<string, object>>> teamTotals,
            string[] categories, int teamCount)
        {
            var points = Enumerable.Range(1, teamCount).Reverse().ToArray();
            var pitchingPoints = new Dictionary<string, List<TeamCategoryResult>>();

            foreach (var cat in categories)
            {
                var categoryResults = new List<TeamCategoryResult>();

                foreach (var teamId in teamTotals.Keys)
                {
                    var team = teamTotals[teamId]["pitch"];
                    decimal statValue = 0;

                    if (cat == "ERA")
                    {
                        int er = team.ContainsKey("ER") ? (int)team["ER"] : 0;
                        decimal ip = team.ContainsKey("IP") ? (decimal)team["IP"] : 0;
                        statValue = ip > 0 ? (9 * er) / ip : 99999;
                    }
                    else if (cat == "WHIP")
                    {
                        int h = team.ContainsKey("H") ? (int)team["H"] : 0;
                        int bb = team.ContainsKey("BB") ? (int)team["BB"] : 0;
                        decimal ip = team.ContainsKey("IP") ? (decimal)team["IP"] : 0;
                        statValue = ip > 0 ? (decimal)(bb + h) / ip : 99999;
                    }
                    else if (cat == "IP")
                    {
                        statValue = team.ContainsKey("IP") ? (decimal)team["IP"] : 0;
                    }
                    else
                    {
                        var mappedStat = MapCategoryToStat(cat);
                        statValue = team.ContainsKey(mappedStat) ? (int)team[mappedStat] : 0;
                    }

                    categoryResults.Add(new TeamCategoryResult
                    {
                        TeamId = teamId,
                        Value = statValue
                    });
                }

                // Sort appropriately (ERA and WHIP are ascending, others descending)
                bool ascending = cat == "ERA" || cat == "WHIP";
                var sortedResults = ascending 
                    ? categoryResults.OrderBy(x => x.Value).ToList()
                    : categoryResults.OrderByDescending(x => x.Value).ToList();

                AssignPointsWithTies(sortedResults, points);
                pitchingPoints[cat] = sortedResults;
            }

            return pitchingPoints;
        }

        private string MapCategoryToStat(string category)
        {
            return category switch
            {
                "R" => "R",
                "RBI" => "RBI",
                "SB" => "SB",
                "HR" => "HR",
                "K" => "K",
                "QS" => "QS",
                "S" => "S",
                "SV" => "SV",
                _ => category
            };
        }

        private void AssignPointsWithTies(List<TeamCategoryResult> sortedResults, int[] points)
        {
            for (int i = 0; i < sortedResults.Count; )
            {
                var currentValue = sortedResults[i].Value;
                int tieCount = 1;

                // Count ties
                while (i + tieCount < sortedResults.Count && sortedResults[i + tieCount].Value == currentValue)
                {
                    tieCount++;
                }

                if (tieCount > 1)
                {
                    // Calculate average points for tied teams
                    decimal totalPoints = 0;
                    for (int p = 0; p < tieCount; p++)
                    {
                        totalPoints += points[i + p];
                    }
                    decimal avgPoints = totalPoints / tieCount;

                    // Assign average points to all tied teams
                    for (int t = 0; t < tieCount; t++)
                    {
                        sortedResults[i + t].Points = Math.Round(avgPoints, 1);
                    }
                }
                else
                {
                    sortedResults[i].Points = points[i];
                }

                i += tieCount;
            }
        }

        private List<Standings> BuildStandingsFromPoints(
            Dictionary<string, List<TeamCategoryResult>> hittingPoints,
            Dictionary<string, List<TeamCategoryResult>> pitchingPoints,
            List<SeasonTeam> teams, string year, DateTime date)
        {
            var standings = new List<Standings>();

            foreach (var team in teams)
            {
                var standing = new Standings
                {
                    Year = year,
                    Date = date,
                    TeamId = team.TeamId,
                    TeamName = team.TeamName,
                    Manager = team.Manager,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                };

                // Set hitting values and points
                foreach (var cat in hittingPoints.Keys)
                {
                    var teamResult = hittingPoints[cat].First(x => x.TeamId == team.TeamId);
                    SetStandingProperty(standing, cat, teamResult.Value, teamResult.Points);
                    standing.TotalHittingPoints += teamResult.Points;
                }

                // Set pitching values and points
                foreach (var cat in pitchingPoints.Keys)
                {
                    var teamResult = pitchingPoints[cat].First(x => x.TeamId == team.TeamId);
                    SetStandingProperty(standing, cat, teamResult.Value, teamResult.Points);
                    standing.TotalPitchingPoints += teamResult.Points;
                }

                standing.TotalPoints = standing.TotalHittingPoints + standing.TotalPitchingPoints;
                standings.Add(standing);
            }

            // Assign overall ranks
            var sortedStandings = standings.OrderByDescending(s => s.TotalPoints).ToList();
            for (int i = 0; i < sortedStandings.Count; i++)
            {
                sortedStandings[i].OverallRank = i + 1;
            }

            // Assign hitting ranks
            var sortedByHitting = standings.OrderByDescending(s => s.TotalHittingPoints).ToList();
            for (int i = 0; i < sortedByHitting.Count; i++)
            {
                sortedByHitting[i].HittingRank = i + 1;
            }

            // Assign pitching ranks
            var sortedByPitching = standings.OrderByDescending(s => s.TotalPitchingPoints).ToList();
            for (int i = 0; i < sortedByPitching.Count; i++)
            {
                sortedByPitching[i].PitchingRank = i + 1;
            }

            return standings.OrderBy(s => s.OverallRank).ToList();
        }

        private void SetStandingProperty(Standings standing, string category, decimal value, decimal points)
        {
            switch (category)
            {
                case "AVG":
                    standing.AVG = Math.Round(value, 3);
                    standing.AVG_Points = points;
                    break;
                case "OPS":
                    standing.OPS = Math.Round(value, 3);
                    standing.OPS_Points = points;
                    break;
                case "R":
                    standing.R = (int)value;
                    standing.R_Points = points;
                    break;
                case "RBI":
                    standing.RBI = (int)value;
                    standing.RBI_Points = points;
                    break;
                case "SB":
                    standing.SB = (int)value;
                    standing.SB_Points = points;
                    break;
                case "HR":
                    standing.HR = (int)value;
                    standing.HR_Points = points;
                    break;
                case "ERA":
                    standing.ERA = Math.Round(value, 2);
                    standing.ERA_Points = points;
                    break;
                case "WHIP":
                    standing.WHIP = Math.Round(value, 3);
                    standing.WHIP_Points = points;
                    break;
                case "K":
                    standing.K = (int)value;
                    standing.K_Points = points;
                    break;
                case "IP":
                    standing.IP = Math.Round(value, 1);
                    standing.IP_Points = points;
                    break;
                case "QS":
                    standing.QS = (int)value;
                    standing.QS_Points = points;
                    break;
                case "S":
                case "SV":
                    standing.S = (int)value;
                    standing.S_Points = points;
                    break;
            }
        }

        // PERFORMANCE OPTIMIZATION METHODS

        private async Task<SeasonBoxScoreData?> LoadAllSeasonBoxScores(string year, DateTime startDate, DateTime endDate)
        {
            try
            {
                var data = new SeasonBoxScoreData();

                // Load all hitting box scores for the season
                var hitCollection = _db.BoxScores[year]["hitting"];
                var hitFilter = Builders<Box>.Filter.And(
                    Builders<Box>.Filter.Eq("player", "TOT"),
                    Builders<Box>.Filter.Eq("position", "A"),
                    Builders<Box>.Filter.Gte("stats_date", startDate.ToString("yyyy-MM-dd")),
                    Builders<Box>.Filter.Lte("stats_date", endDate.ToString("yyyy-MM-dd"))
                );
                data.HittingBoxScores = await hitCollection.Find(hitFilter).ToListAsync();

                // Load all pitching box scores for the season
                var pitchCollection = _db.BoxScores[year]["pitching"];
                var pitchFilter = Builders<Box>.Filter.And(
                    Builders<Box>.Filter.Eq("player", "TOT"),
                    Builders<Box>.Filter.Eq("position", "A"),
                    Builders<Box>.Filter.Gte("stats_date", startDate.ToString("yyyy-MM-dd")),
                    Builders<Box>.Filter.Lte("stats_date", endDate.ToString("yyyy-MM-dd"))
                );
                data.PitchingBoxScores = await pitchCollection.Find(pitchFilter).ToListAsync();

                // Load quality appearance data (individual pitcher stats)
                var qaFilter = Builders<Box>.Filter.And(
                    Builders<Box>.Filter.Eq("position", "P"),
                    Builders<Box>.Filter.Ne("player", "TOT"),
                    Builders<Box>.Filter.Gte("stats_date", startDate.ToString("yyyy-MM-dd")),
                    Builders<Box>.Filter.Lte("stats_date", endDate.ToString("yyyy-MM-dd"))
                );
                data.QualityAppearanceBoxScores = await pitchCollection.Find(qaFilter).ToListAsync();

                // Group data by date for efficient daily processing
                data.HittingByDate = data.HittingBoxScores
                    .Where(b => !string.IsNullOrEmpty(b.StatsDate))
                    .GroupBy(b => b.StatsDate!)
                    .ToDictionary(g => g.Key, g => g.ToList());

                data.PitchingByDate = data.PitchingBoxScores
                    .Where(b => !string.IsNullOrEmpty(b.StatsDate))
                    .GroupBy(b => b.StatsDate!)
                    .ToDictionary(g => g.Key, g => g.ToList());

                data.QualityAppearanceByDate = data.QualityAppearanceBoxScores
                    .Where(b => !string.IsNullOrEmpty(b.StatsDate))
                    .GroupBy(b => b.StatsDate!)
                    .ToDictionary(g => g.Key, g => g.ToList());

                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading season box scores: {ex.Message}");
                return null;
            }
        }

        private async Task<List<Standings>> ProcessIncrementalStandings(
            string year, List<SeasonTeam> teams, DateTime startDate, DateTime endDate, 
            SeasonBoxScoreData seasonData, string? progressGroupId, IProgress<string>? progress, 
            CancellationToken cancellationToken)
        {
            var allStandings = new List<Standings>();
            
            // Initialize running totals for all teams
            var runningTotals = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();
            foreach (var team in teams)
            {
                runningTotals[team.TeamId!] = new Dictionary<string, Dictionary<string, object>>
                {
                    ["hit"] = new Dictionary<string, object>(),
                    ["pitch"] = new Dictionary<string, object>()
                };
            }

            var hittingCategories = new[] { "AVG", "OPS", "R", "SB", "HR", "RBI" };
            var pitchingCategories = year == "2019" 
                ? new[] { "ERA", "WHIP", "IP", "K", "S", "QS" }
                : new[] { "ERA", "WHIP", "IP", "K", "SV", "QS" };

            // Process each day incrementally
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var dateStr = date.ToString("yyyy-MM-dd");
                var progressMessage = $"Processing standings for {dateStr}...";
                
                if (!string.IsNullOrEmpty(progressGroupId))
                {
                    await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("ProgressUpdate", progressMessage);
                }
                progress?.Report(progressMessage);

                // Add daily hitting data to running totals
                if (seasonData.HittingByDate.TryGetValue(dateStr, out var dailyHitting))
                {
                    ProcessDailyHittingData(dailyHitting, runningTotals);
                }

                // Add daily pitching data to running totals
                if (seasonData.PitchingByDate.TryGetValue(dateStr, out var dailyPitching))
                {
                    ProcessDailyPitchingData(dailyPitching, runningTotals, year);
                }

                // Add daily quality appearances to running totals
                if (seasonData.QualityAppearanceByDate.TryGetValue(dateStr, out var dailyQA))
                {
                    ProcessDailyQualityAppearances(dailyQA, runningTotals);
                }

                // Calculate standings using current running totals
                var hittingPoints = CalculateHittingPoints(runningTotals, hittingCategories, teams.Count);
                var pitchingPoints = CalculatePitchingPoints(runningTotals, pitchingCategories, teams.Count);
                var dailyStandings = BuildStandingsFromPoints(hittingPoints, pitchingPoints, teams, year, date);
                
                allStandings.AddRange(dailyStandings);

                // Small delay for progress and cancellation checking
                await Task.Delay(1, cancellationToken);
            }

            return allStandings;
        }

        private void ProcessDailyHittingData(List<Box> dailyBoxScores, 
            Dictionary<string, Dictionary<string, Dictionary<string, object>>> runningTotals)
        {
            var hStats = new[] { "2B", "3B", "AB", "BB", "H", "HBP", "HR", "PA", "R", "RBI", "SB", "SF" };
            
            foreach (var score in dailyBoxScores)
            {
                if (score.TeamId != null && runningTotals.ContainsKey(score.TeamId))
                {
                    var team = runningTotals[score.TeamId]["hit"];
                    
                    foreach (var stat in hStats)
                    {
                        var value = GetHittingStatValue(score, stat);
                        if (value.HasValue)
                        {
                            if (team.ContainsKey(stat))
                            {
                                team[stat] = (int)team[stat] + value.Value;
                            }
                            else
                            {
                                team[stat] = value.Value;
                            }
                        }
                    }
                }
            }
        }

        private void ProcessDailyPitchingData(List<Box> dailyBoxScores, 
            Dictionary<string, Dictionary<string, Dictionary<string, object>>> runningTotals, string year)
        {
            var pStats = year == "2019" 
                ? new[] { "BB", "ER", "H", "HB", "IP", "K", "QS", "S" }
                : new[] { "BB", "ER", "H", "HB", "IP", "K", "QS", "SV" };

            foreach (var score in dailyBoxScores)
            {
                if (score.TeamId != null && runningTotals.ContainsKey(score.TeamId))
                {
                    var team = runningTotals[score.TeamId]["pitch"];
                    
                    foreach (var stat in pStats)
                    {
                        if (stat == "IP")
                        {
                            // Handle IP special case
                            if (!string.IsNullOrEmpty(score.InningsPitched))
                            {
                                decimal innings = decimal.Parse(score.InningsPitched);
                                
                                // Handle 2019 special IP calculation
                                if (year == "2019")
                                {
                                    decimal partialInnings = innings % 1 * 3.3m;
                                    decimal fullInnings = Math.Floor(innings);
                                    innings = fullInnings + partialInnings;
                                }

                                if (team.ContainsKey(stat))
                                {
                                    decimal currentTotal = (decimal)team[stat];
                                    decimal newTotal = currentTotal + innings;
                                    decimal outs = Math.Round(newTotal * 3);
                                    decimal newIP = Math.Round(outs / 3, 2);
                                    team[stat] = newIP;
                                }
                                else
                                {
                                    team[stat] = innings;
                                }
                            }
                        }
                        else
                        {
                            var value = GetPitchingStatValue(score, stat);
                            if (value.HasValue)
                            {
                                if (team.ContainsKey(stat))
                                {
                                    team[stat] = (int)team[stat] + value.Value;
                                }
                                else
                                {
                                    team[stat] = value.Value;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ProcessDailyQualityAppearances(List<Box> dailyBoxScores, 
            Dictionary<string, Dictionary<string, Dictionary<string, object>>> runningTotals)
        {
            foreach (var score in dailyBoxScores)
            {
                if (score.TeamId != null && runningTotals.ContainsKey(score.TeamId))
                {
                    if (!string.IsNullOrEmpty(score.InningsPitched) && score.EarnedRuns != null)
                    {
                        decimal ip = decimal.Parse(score.InningsPitched);
                        var er = ConvertToInt(score.EarnedRuns);
                        
                        if (ip >= 5 && er.HasValue)
                        {
                            decimal era = ip > 0 ? (9 * er.Value) / ip : 99999;
                            if (era <= 4.5m)
                            {
                                var team = runningTotals[score.TeamId]["pitch"];
                                if (team.ContainsKey("QA"))
                                {
                                    team["QA"] = (int)team["QA"] + 1;
                                }
                                else
                                {
                                    team["QA"] = 1;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Generate compiled documents for optimized performance
        /// Replaces thousands of individual documents with 2 optimized documents
        /// </summary>
        private async Task GenerateCompiledDocumentsAsync(string year, List<Standings> allStandings, CancellationToken cancellationToken)
        {
            var compilationStart = DateTime.UtcNow;
            
            try
            {
                Console.WriteLine($"[CompiledStandings] Starting compilation for year {year} with {allStandings.Count} standings records");
                
                cancellationToken.ThrowIfCancellationRequested();

                // Extract final standings (latest date for each team)
                var finalStandings = allStandings
                    .GroupBy(s => s.TeamId)
                    .Select(g => g.OrderByDescending(s => s.Date).First())
                    .OrderByDescending(s => s.TotalPoints)
                    .ToList();

                // Create compiled final standings document
                var compiledFinal = new CompiledFinalStandings
                {
                    Year = year,
                    Type = "final_standings",
                    CompiledAt = DateTime.UtcNow,
                    SourceLastUpdated = allStandings.Max(s => s.LastUpdatedAt),
                    FinalStandings = finalStandings,
                    Metadata = new CompilationMetadata
                    {
                        SourceDocumentsProcessed = allStandings.Count,
                        TeamsCount = finalStandings.Count,
                        DateRangeStart = allStandings.Min(s => s.Date),
                        DateRangeEnd = allStandings.Max(s => s.Date),
                        CompilationDurationMs = 0, // Will be set after completion
                        CompilationVersion = "1.0"
                    }
                };

                // Create compiled progression data document
                var progressionData = allStandings
                    .OrderBy(s => s.Date)
                    .ThenBy(s => s.TeamId)
                    .ToList();

                var compiledProgression = new CompiledProgressionData
                {
                    Year = year,
                    Type = "progression_data",
                    CompiledAt = DateTime.UtcNow,
                    SourceLastUpdated = allStandings.Max(s => s.LastUpdatedAt),
                    ProgressionData = progressionData,
                    Metadata = new CompilationMetadata
                    {
                        SourceDocumentsProcessed = allStandings.Count,
                        TeamsCount = finalStandings.Count,
                        DateRangeStart = allStandings.Min(s => s.Date),
                        DateRangeEnd = allStandings.Max(s => s.Date),
                        CompilationDurationMs = 0, // Will be set after completion
                        CompilationVersion = "1.0"
                    }
                };

                cancellationToken.ThrowIfCancellationRequested();

                // Save compiled final standings
                var finalCollection = _db.CompiledFinalStandings[year];
                var finalFilter = Builders<CompiledFinalStandings>.Filter.And(
                    Builders<CompiledFinalStandings>.Filter.Eq("year", year),
                    Builders<CompiledFinalStandings>.Filter.Eq("type", "final_standings")
                );

                var existingFinal = await finalCollection.Find(finalFilter).FirstOrDefaultAsync(cancellationToken);
                if (existingFinal != null)
                {
                    // Update existing document
                    compiledFinal.Id = existingFinal.Id;
                    await finalCollection.ReplaceOneAsync(f => f.Id == compiledFinal.Id, compiledFinal, cancellationToken: cancellationToken);
                    Console.WriteLine($"[CompiledStandings] Updated existing final standings document for {year}");
                }
                else
                {
                    // Insert new document
                    await finalCollection.InsertOneAsync(compiledFinal, cancellationToken: cancellationToken);
                    Console.WriteLine($"[CompiledStandings] Created new final standings document for {year}");
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Save compiled progression data
                var progressionCollection = _db.CompiledProgressionData[year];
                var progressionFilter = Builders<CompiledProgressionData>.Filter.And(
                    Builders<CompiledProgressionData>.Filter.Eq("year", year),
                    Builders<CompiledProgressionData>.Filter.Eq("type", "progression_data")
                );

                var existingProgression = await progressionCollection.Find(progressionFilter).FirstOrDefaultAsync(cancellationToken);
                if (existingProgression != null)
                {
                    // Update existing document
                    compiledProgression.Id = existingProgression.Id;
                    await progressionCollection.ReplaceOneAsync(p => p.Id == compiledProgression.Id, compiledProgression, cancellationToken: cancellationToken);
                    Console.WriteLine($"[CompiledStandings] Updated existing progression data document for {year}");
                }
                else
                {
                    // Insert new document
                    await progressionCollection.InsertOneAsync(compiledProgression, cancellationToken: cancellationToken);
                    Console.WriteLine($"[CompiledStandings] Created new progression data document for {year}");
                }

                var compilationEnd = DateTime.UtcNow;
                var duration = (compilationEnd - compilationStart).TotalMilliseconds;

                Console.WriteLine($"[CompiledStandings] Successfully compiled standings for {year}:");
                Console.WriteLine($"  - Final standings: {finalStandings.Count} teams");
                Console.WriteLine($"  - Progression data: {progressionData.Count} records");
                Console.WriteLine($"  - Source documents: {allStandings.Count} → 2 compiled documents");
                Console.WriteLine($"  - Compilation time: {duration:F0}ms");
                Console.WriteLine($"  - Performance improvement: {(allStandings.Count / 2.0):F0}x fewer documents to query");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[CompiledStandings] Compilation cancelled for year {year}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CompiledStandings] Error compiling standings for year {year}: {ex.Message}");
                Console.WriteLine($"[CompiledStandings] Stack trace: {ex.StackTrace}");
                // Don't throw - compilation failure shouldn't break standings calculation
                // The system will fall back to individual document queries
            }
        }
    }

    public class TeamCategoryResult
    {
        public string TeamId { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal Points { get; set; }
    }

    public class SeasonBoxScoreData
    {
        public List<Box> HittingBoxScores { get; set; } = new List<Box>();
        public List<Box> PitchingBoxScores { get; set; } = new List<Box>();
        public List<Box> QualityAppearanceBoxScores { get; set; } = new List<Box>();
        
        // Grouped by date for efficient daily processing
        public Dictionary<string, List<Box>> HittingByDate { get; set; } = new Dictionary<string, List<Box>>();
        public Dictionary<string, List<Box>> PitchingByDate { get; set; } = new Dictionary<string, List<Box>>();
        public Dictionary<string, List<Box>> QualityAppearanceByDate { get; set; } = new Dictionary<string, List<Box>>();
    }
}
