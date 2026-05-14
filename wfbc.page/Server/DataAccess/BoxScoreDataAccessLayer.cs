using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using WFBC.Server.Models;
using WFBC.Server.Interface;
using MongoDB.Driver;
using MongoDB.Bson;

namespace WFBC.Server.DataAccess
{
    public class BoxScoreDataAccessLayer : IBoxScore
    {
        private readonly WfbcDBContext _db;
        private static readonly string[] HittingCats = { "2B","3B","AB","AVG","BB","CS","GP","H","HBP","HR","K","OPS","PA","R","RBI","SB","SF" };
        private static readonly string[] PitchingCats = { "BB","BS","ER","ERA","GP","GS","H","HB","HR","IP","K","L","QS","R","SV","W","WHIP","WP" };

        public BoxScoreDataAccessLayer(WfbcDBContext db) { _db = db; }

        public async Task<BoxScoreImportResult> ImportBoxScores(string year, List<BoxScoreEntry> entries, string type)
        {
            var result = new BoxScoreImportResult();
            if (!_db.BoxScores.ContainsKey(year)) { result.Errors.Add($"No DB for year {year}"); return result; }

            var collection = _db.BoxScores[year][type];
            string[] cats = type == "hitting" ? HittingCats : PitchingCats;
            var toInsert = new List<BsonDocument>();

            foreach (var entry in entries)
            {
                try
                {
                    var teamId = entry.GetValueOrDefault("teamID")?.ToString();
                    var statsDate = entry.GetValueOrDefault("stats_date")?.ToString();
                    var player = entry.GetValueOrDefault("player")?.ToString();
                    var position = entry.GetValueOrDefault("position")?.ToString();
                    if (string.IsNullOrEmpty(teamId) || string.IsNullOrEmpty(statsDate) || string.IsNullOrEmpty(player))
                    { result.SkippedEntries++; continue; }

                    var bsonDoc = ConvertToBson(entry);
                    var filter = Builders<BsonDocument>.Filter.And(
                        Builders<BsonDocument>.Filter.Eq("teamID", teamId),
                        Builders<BsonDocument>.Filter.Eq("stats_date", statsDate),
                        Builders<BsonDocument>.Filter.Eq("player", player),
                        Builders<BsonDocument>.Filter.Eq("position", position));

                    var existing = await collection.Find(filter).FirstOrDefaultAsync();
                    if (existing == null) { toInsert.Add(bsonDoc); result.NewEntries++; }
                    else
                    {
                        var updates = new List<UpdateDefinition<BsonDocument>>();
                        var log = existing.Contains("updates") ? existing["updates"].AsBsonArray.Select(x => x.AsString).ToList() : new List<string>();
                        bool changed = false;
                        bool isTotRow = player == "TOT" && position == "A";
                        foreach (var cat in cats)
                        {
                            var nv = bsonDoc.Contains(cat) ? bsonDoc[cat] : BsonNull.Value;
                            var ev = existing.Contains(cat) ? existing[cat] : BsonNull.Value;
                            // Use value-based comparison to avoid BSON type mismatch false positives
                            bool valuesEqual = BsonValueEquals(nv, ev);
                            if (!valuesEqual) { changed = true; updates.Add(Builders<BsonDocument>.Update.Set(cat, nv)); log.Add($"{DateTime.UtcNow:u}: {cat} {ev}->{nv}"); }
                            if (isTotRow && !valuesEqual)
                            {
                                Console.WriteLine($"[BoxScore] TOT/A UPDATE {type} team={teamId} date={statsDate} {cat}: {ev} ({ev?.BsonType}) -> {nv} ({nv?.BsonType})");
                            }
                        }
                        if (changed)
                        {
                            updates.Add(Builders<BsonDocument>.Update.Set("updates", new BsonArray(log)));
                            // Also update download_date to reflect when data was last fetched
                            if (bsonDoc.Contains("download_date"))
                                updates.Add(Builders<BsonDocument>.Update.Set("download_date", bsonDoc["download_date"]));
                            await collection.UpdateOneAsync(Builders<BsonDocument>.Filter.Eq("_id", existing["_id"]), Builders<BsonDocument>.Update.Combine(updates));
                            result.UpdatedEntries++;
                            if (isTotRow) Console.WriteLine($"[BoxScore] TOT/A record UPDATED: {type} team={teamId} date={statsDate}");
                        }
                        else
                        {
                            result.SkippedEntries++;
                            if (isTotRow) Console.WriteLine($"[BoxScore] TOT/A record SKIPPED (no changes): {type} team={teamId} date={statsDate}");
                        }
                    }
                }
                catch (Exception ex) { result.Errors.Add(ex.Message); }
            }
            if (toInsert.Count > 0) await collection.InsertManyAsync(toInsert);
            return result;
        }

        private BsonDocument ConvertToBson(BoxScoreEntry entry)
        {
            var doc = new BsonDocument();
            foreach (var kvp in entry)
            {
                if (kvp.Value == null) { doc[kvp.Key] = BsonNull.Value; continue; }
                if (kvp.Value is System.Text.Json.JsonElement je)
                    doc[kvp.Key] = BsonValue.Create(ParseJson(je));
                else
                    doc[kvp.Key] = BsonValue.Create(kvp.Value);
            }
            return doc;
        }

        /// <summary>
        /// Compare two BsonValues by their numeric/string value, ignoring BSON type differences
        /// (e.g., BsonInt32(5) == BsonInt64(5) == BsonDouble(5.0))
        /// </summary>
        private static bool BsonValueEquals(BsonValue a, BsonValue b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            if (a.BsonType == b.BsonType) return a == b;
            // Both are numeric — compare as double
            if (a.IsNumeric && b.IsNumeric) return a.ToDouble() == b.ToDouble();
            // Compare as string representation as fallback
            return a.ToString() == b.ToString();
        }

        private object? ParseJson(System.Text.Json.JsonElement je) => je.ValueKind switch
        {
            System.Text.Json.JsonValueKind.String => je.GetString(),
            System.Text.Json.JsonValueKind.Number => je.TryGetInt64(out long l) ? (object)l : je.GetDouble(),
            System.Text.Json.JsonValueKind.True => true,
            System.Text.Json.JsonValueKind.False => false,
            _ => null
        };
    }
}