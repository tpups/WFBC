using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using WFBC.Server.Models;
using WFBC.Server.Interface;
using MongoDB.Driver;

namespace WFBC.Server.DataAccess
{
    public class StandingsDataAccessLayer : IStandings
    {
        private WfbcDBContext db;
        public StandingsDataAccessLayer(WfbcDBContext _db)
        {
            db = _db;
        }

        // Get the standings on a specific day
        public Standings GetStandingsByDate(DateTime date)
        {
            try
            {
                string year = date.Year.ToString();
                FilterDefinition<Standings> filterStandingsData = Builders<Standings>.Filter.Eq("Date", date);
                return db.Standings[year].Find(filterStandingsData).FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }

        // Save multiple standings records to the database
        public async Task SaveStandingsAsync(List<Standings> standings, string year)
        {
            try
            {
                Console.WriteLine($"[SaveStandingsAsync] Called with {standings?.Count ?? 0} standings for year {year}");
                
                if (standings == null || !standings.Any())
                {
                    Console.WriteLine($"[SaveStandingsAsync] No standings to save - returning early");
                    return;
                }

                var collection = db.Standings[year];
                var currentTime = DateTime.UtcNow;

                Console.WriteLine($"[SaveStandingsAsync] Collection: wfbc{year}.standings");

                // Set timestamps for all records
                foreach (var standing in standings)
                {
                    if (standing.CreatedAt == null)
                        standing.CreatedAt = currentTime;
                    standing.LastUpdatedAt = currentTime;
                }

                Console.WriteLine($"[SaveStandingsAsync] Processing {standings.Count} standings...");

                int insertCount = 0;
                int updateCount = 0;

                // Use individual operations like the working Pick pattern to avoid bulk write ObjectId issues
                foreach (var standing in standings)
                {
                    // Create filter that identifies unique standings (Year + Date + TeamId)
                    var filter = Builders<Standings>.Filter.And(
                        Builders<Standings>.Filter.Eq("Year", standing.Year),
                        Builders<Standings>.Filter.Eq("Date", standing.Date),
                        Builders<Standings>.Filter.Eq("TeamId", standing.TeamId)
                    );

                    // Check if record already exists
                    var existingRecord = await collection.Find(filter).FirstOrDefaultAsync();
                    
                    if (existingRecord != null)
                    {
                        // Update existing - use ReplaceOne with existing Id (like DraftDataAccessLayer pattern)
                        standing.Id = existingRecord.Id; // Keep existing Id
                        var replaceResult = await collection.ReplaceOneAsync(p => p.Id == standing.Id, standing);
                        Console.WriteLine($"[SaveStandingsAsync] Updated existing record: {replaceResult.ModifiedCount} modified");
                        updateCount++;
                    }
                    else 
                    {
                        // Insert new - let MongoDB generate Id (like PickDataAccessLayer pattern)
                        standing.Id = null; // Clear any Id
                        await collection.InsertOneAsync(standing);
                        Console.WriteLine($"[SaveStandingsAsync] Inserted new record with Id: {standing.Id}");
                        insertCount++;
                    }
                }

                Console.WriteLine($"[SaveStandingsAsync] Completed: {insertCount} inserts, {updateCount} updates");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SaveStandingsAsync] ERROR: {ex.Message}");
                Console.WriteLine($"[SaveStandingsAsync] Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        // Get information about existing standings for a year
        public async Task<StandingsInfo> GetExistingStandingsInfoAsync(string year)
        {
            try
            {
                var collection = db.Standings[year];
                var filter = Builders<Standings>.Filter.Eq("Year", year);
                
                var count = await collection.CountDocumentsAsync(filter);
                
                if (count == 0)
                {
                    return new StandingsInfo 
                    { 
                        Exist = false, 
                        LastUpdated = null, 
                        RecordCount = 0 
                    };
                }

                // Get the most recent LastUpdatedAt timestamp
                var latestRecord = await collection
                    .Find(filter)
                    .SortByDescending(s => s.LastUpdatedAt)
                    .Limit(1)
                    .FirstOrDefaultAsync();

                return new StandingsInfo
                {
                    Exist = true,
                    LastUpdated = latestRecord?.LastUpdatedAt,
                    RecordCount = (int)count
                };
            }
            catch
            {
                throw;
            }
        }

        // Delete all standings for a specific year
        public async Task DeleteAllStandingsForYearAsync(string year)
        {
            try
            {
                var collection = db.Standings[year];
                var filter = Builders<Standings>.Filter.Eq("Year", year);
                await collection.DeleteManyAsync(filter);
            }
            catch
            {
                throw;
            }
        }

        // Check if standings exist for a specific year
        public async Task<bool> StandingsExistForYearAsync(string year)
        {
            try
            {
                var collection = db.Standings[year];
                var filter = Builders<Standings>.Filter.Eq("Year", year);
                var count = await collection.CountDocumentsAsync(filter, new CountOptions { Limit = 1 });
                return count > 0;
            }
            catch
            {
                throw;
            }
        }
    }
}
