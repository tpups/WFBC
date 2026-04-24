using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using WFBC.Shared.Models;
using WFBC.Server.Models;

namespace WFBC.Server.Services
{
    /// <summary>
    /// Service for calculating wager data from box scores.
    /// Wager definitions are hardcoded here; data is compiled alongside standings.
    /// </summary>
    public class WagerService
    {
        private readonly WfbcDBContext _db;

        public WagerService(WfbcDBContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Calculate all wagers for a given year from box score data.
        /// Returns the wagers and the max download_date across all queried box scores.
        /// </summary>
        public async Task<(List<WagerData> Wagers, DateTime? MaxDownloadDate)> CalculateWagersAsync(string year, CancellationToken cancellationToken = default)
        {
            var wagers = new List<WagerData>();
            DateTime? maxDownloadDate = null;

            // Only calculate wagers for 2026+
            if (!int.TryParse(year, out var yearInt) || yearInt < 2026)
                return (wagers, null);

            // --- Wager 1: KG vs KMcG counting stats ---
            var (kgVsKmcg, downloadDate) = await CalculateCountingStatsWager(
                year,
                wagerName: "KG vs KMcG",
                subtitle: "Counting Stats (R, RBI, HR, SB, BB)",
                statCategories: new List<string> { "R", "RBI", "HR", "SB", "BB" },
                players: new[]
                {
                    ("Konnor Griffin", "19501"),
                    ("Kevin McGonigle", "18895")
                },
                cancellationToken);

            if (kgVsKmcg != null)
                wagers.Add(kgVsKmcg);

            if (downloadDate.HasValue && (!maxDownloadDate.HasValue || downloadDate > maxDownloadDate))
                maxDownloadDate = downloadDate;

            return (wagers, maxDownloadDate);
        }

        /// <summary>
        /// Calculate a counting stats wager by summing hitting stats for specific players.
        /// Returns the wager data and max download_date from queried box scores.
        /// </summary>
        private async Task<(WagerData? Wager, DateTime? MaxDownloadDate)> CalculateCountingStatsWager(
            string year,
            string wagerName,
            string subtitle,
            List<string> statCategories,
            (string Name, string NewsId)[] players,
            CancellationToken cancellationToken)
        {
            try
            {
                var hitCollection = _db.BoxScoresTyped[year]["hitting"];
                var newsIds = players.Select(p => p.NewsId).ToList();

                // Query all hitting box scores for these players (non-TOT, active lineup)
                var filter = Builders<Box>.Filter.And(
                    Builders<Box>.Filter.In("newsID", newsIds),
                    Builders<Box>.Filter.Ne("player", "TOT")
                );

                var boxScores = await hitCollection.Find(filter).ToListAsync(cancellationToken);

                Console.WriteLine($"[WagerService] Found {boxScores.Count} box score records for wager '{wagerName}' in {year}");

                // Track max download_date for "last updated" display
                DateTime? maxDownloadDate = null;
                foreach (var box in boxScores)
                {
                    if (!string.IsNullOrEmpty(box.DownloadDate) && DateTime.TryParse(box.DownloadDate, out var dt))
                    {
                        if (!maxDownloadDate.HasValue || dt > maxDownloadDate.Value)
                            maxDownloadDate = dt;
                    }
                }

                var wagerPlayers = new List<WagerPlayer>();

                foreach (var (name, newsId) in players)
                {
                    var playerBoxScores = boxScores.Where(b => b.NewsId == newsId).ToList();
                    var statBreakdown = new Dictionary<string, int>();
                    var total = 0;

                    foreach (var stat in statCategories)
                    {
                        var statValue = 0;
                        foreach (var box in playerBoxScores)
                        {
                            var val = GetHittingStatValue(box, stat);
                            if (val.HasValue)
                                statValue += val.Value;
                        }
                        statBreakdown[stat] = statValue;
                        total += statValue;
                    }

                    Console.WriteLine($"[WagerService] {name} (newsID={newsId}): total={total}, breakdown={string.Join(", ", statBreakdown.Select(kv => $"{kv.Key}={kv.Value}"))}");

                    wagerPlayers.Add(new WagerPlayer
                    {
                        Name = name,
                        NewsId = newsId,
                        Value = total,
                        StatBreakdown = statBreakdown
                    });
                }

                return (new WagerData
                {
                    WagerName = wagerName,
                    Subtitle = subtitle,
                    StatCategories = statCategories,
                    Players = wagerPlayers
                }, maxDownloadDate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WagerService] Error calculating wager '{wagerName}' for {year}: {ex.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// Extract a hitting stat value from a Box record (replicates RotisserieStandingsService pattern)
        /// </summary>
        private static int? GetHittingStatValue(Box box, string stat)
        {
            object? rawValue = stat switch
            {
                "R" => box.Runs,
                "RBI" => box.RunsBattedIn,
                "HR" => box.HomeRuns,
                "SB" => box.StolenBases,
                "BB" => box.Walks,
                "H" => box.Hits,
                "2B" => box.Doubles,
                "3B" => box.Triples,
                "AB" => box.AtBats,
                "PA" => box.PlateAppearances,
                "CS" => box.CaughtStealing,
                "HBP" => box.HitByPitch,
                "SF" => box.SacrificeFlies,
                "K" => box.Strikeouts,
                _ => null
            };

            return ConvertToInt(rawValue);
        }

        /// <summary>
        /// Convert various numeric types to int (same pattern as RotisserieStandingsService)
        /// </summary>
        private static int? ConvertToInt(object? value)
        {
            if (value == null) return null;

            return value switch
            {
                int i => i,
                long l => (int)l,
                double d => (int)d,
                decimal dec => (int)dec,
                short s => (int)s,
                string str when int.TryParse(str, out var parsed) => parsed,
                _ => null
            };
        }

        /// <summary>
        /// Compile wager data and store in the database
        /// </summary>
        public async Task CompileWagerDataAsync(string year, CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine($"[WagerService] Compiling wager data for {year}...");

                var (wagers, maxDownloadDate) = await CalculateWagersAsync(year, cancellationToken);

                if (wagers.Count == 0)
                {
                    Console.WriteLine($"[WagerService] No wagers to compile for {year}");
                    return;
                }

                var compiledWager = new CompiledWagerData
                {
                    Year = year,
                    Type = "wager_data",
                    CompiledAt = DateTime.UtcNow,
                    LastBoxScoreUpdate = maxDownloadDate,
                    Wagers = wagers
                };

                // Store in the compiled_standings collection
                var collection = _db.CompiledWagerData[year];
                var filter = Builders<CompiledWagerData>.Filter.And(
                    Builders<CompiledWagerData>.Filter.Eq("year", year),
                    Builders<CompiledWagerData>.Filter.Eq("type", "wager_data")
                );

                var existing = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);

                if (existing != null)
                {
                    compiledWager.Id = existing.Id;
                    await collection.ReplaceOneAsync(
                        f => f.Id == compiledWager.Id,
                        compiledWager,
                        cancellationToken: cancellationToken);
                    Console.WriteLine($"[WagerService] Updated existing wager data for {year}");
                }
                else
                {
                    await collection.InsertOneAsync(compiledWager, cancellationToken: cancellationToken);
                    Console.WriteLine($"[WagerService] Created new wager data for {year}");
                }

                Console.WriteLine($"[WagerService] Successfully compiled {wagers.Count} wagers for {year}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[WagerService] Wager compilation cancelled for {year}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WagerService] Error compiling wagers for {year}: {ex.Message}");
                // Don't throw - wager compilation failure shouldn't break standings
            }
        }
    }
}