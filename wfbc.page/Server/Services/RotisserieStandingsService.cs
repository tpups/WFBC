using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using WFBC.Shared.Models;
using WFBC.Server.Models;
using Microsoft.AspNetCore.SignalR;
using WFBC.Server.Hubs;

namespace WFBC.Server.Services
{
    public class RotisserieStandingsService
    {
        private readonly WfbcDBContext _db;
        private readonly IHubContext<ProgressHub> _hubContext;

        public RotisserieStandingsService(WfbcDBContext db, IHubContext<ProgressHub> hubContext)
        {
            _db = db;
            _hubContext = hubContext;
        }

        public async Task<List<Standings>> CalculateStandingsForDate(string year, DateTime date, List<Team> teams)
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

        public async Task<List<Standings>> CalculateStandingsForSeason(string year, List<Team> teams, string? progressGroupId = null, IProgress<string>? progress = null)
        {
            var startDate = new DateTime(int.Parse(year), 1, 1);
            var endDate = new DateTime(int.Parse(year), 12, 31);
            var allStandings = new List<Standings>();

            var startMessage = $"Starting calculation for {year} season...";
            if (!string.IsNullOrEmpty(progressGroupId))
            {
                await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("ProgressUpdate", startMessage);
            }
            progress?.Report(startMessage);

            // Calculate daily standings for the entire season
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var progressMessage = $"Processing standings for {date:yyyy-MM-dd}...";
                if (!string.IsNullOrEmpty(progressGroupId))
                {
                    await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("ProgressUpdate", progressMessage);
                }
                progress?.Report(progressMessage);
                
                var dailyStandings = await CalculateStandingsForDate(year, date, teams);
                allStandings.AddRange(dailyStandings);
                
                // Small delay to allow progress updates to be processed
                await Task.Delay(1);
            }

            var completedMessage = $"Completed calculation for {year} season!";
            if (!string.IsNullOrEmpty(progressGroupId))
            {
                await _hubContext.Clients.Group($"progress-{progressGroupId}").SendAsync("ProgressUpdate", completedMessage);
            }
            progress?.Report(completedMessage);
            
            return allStandings;
        }

        private async Task<Dictionary<string, Dictionary<string, Dictionary<string, object>>>> GetTeamTotals(
            string year, List<Team> teams, DateTime startDate, DateTime endDate)
        {
            try
            {
                var totals = new Dictionary<string, Dictionary<string, Dictionary<string, object>>>();
                
                foreach (var team in teams)
                {
                    totals[team.Id!] = new Dictionary<string, Dictionary<string, object>>
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
                "SV" => box.Saves,
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
            List<Team> teams, string year, DateTime date)
        {
            var standings = new List<Standings>();

            foreach (var team in teams)
            {
                var standing = new Standings
                {
                    Year = year,
                    Date = date,
                    TeamId = team.Id,
                    TeamName = team.Name,
                    Manager = team.ManagerId,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdatedAt = DateTime.UtcNow
                };

                // Set hitting values and points
                foreach (var cat in hittingPoints.Keys)
                {
                    var teamResult = hittingPoints[cat].First(x => x.TeamId == team.Id);
                    SetStandingProperty(standing, cat, teamResult.Value, teamResult.Points);
                    standing.TotalHittingPoints += teamResult.Points;
                }

                // Set pitching values and points
                foreach (var cat in pitchingPoints.Keys)
                {
                    var teamResult = pitchingPoints[cat].First(x => x.TeamId == team.Id);
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
    }

    public class TeamCategoryResult
    {
        public string TeamId { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public decimal Points { get; set; }
    }
}
