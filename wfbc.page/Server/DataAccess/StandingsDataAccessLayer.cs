using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        // Get final standings for a year (latest date for each team)
        public async Task<List<Standings>> GetFinalStandingsForYearAsync(string year)
        {
            try
            {
                Console.WriteLine($"[GetFinalStandingsForYearAsync] Querying standings for year {year}");
                
                var collection = db.Standings[year];
                
                // Direct query with longer timeout for existing collections with data
                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                
                var allStandings = await collection
                    .Find(Builders<Standings>.Filter.Eq("Year", year))
                    .ToListAsync(cancellationTokenSource.Token);

                if (!allStandings.Any())
                {
                    Console.WriteLine($"[GetFinalStandingsForYearAsync] No standings found for year {year}");
                    return new List<Standings>();
                }

                // Group by TeamId and get the latest date for each team
                var finalStandings = allStandings
                    .GroupBy(s => s.TeamId)
                    .Select(g => g.OrderByDescending(s => s.Date).First())
                    .OrderByDescending(s => s.TotalPoints)
                    .ToList();

                Console.WriteLine($"[GetFinalStandingsForYearAsync] Found {finalStandings.Count} teams for year {year}");
                return finalStandings;
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"[GetFinalStandingsForYearAsync] Timeout for year {year}: {ex.Message}");
                return new List<Standings>();
            }
            catch (MongoException ex)
            {
                Console.WriteLine($"[GetFinalStandingsForYearAsync] MongoDB error for year {year}: {ex.Message}");
                return new List<Standings>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetFinalStandingsForYearAsync] Error for year {year}: {ex.Message}");
                return new List<Standings>();
            }
        }

        // Get all standings data for progression (graph) display
        public async Task<List<Standings>> GetStandingsProgressionForYearAsync(string year)
        {
            try
            {
                Console.WriteLine($"[GetStandingsProgressionForYearAsync] Querying progression data for year {year}");
                
                // Check if collection exists (without DatabaseExistsAsync call)
                if (!await CollectionExistsAsync(year))
                {
                    Console.WriteLine($"[GetStandingsProgressionForYearAsync] Standings collection doesn't exist for year {year}");
                    return new List<Standings>();
                }

                var collection = db.Standings[year];
                var filter = Builders<Standings>.Filter.Eq("Year", year);
                
                // Get all standings ordered by date for progression display - longer timeout for large dataset
                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(60));
                
                var standings = await collection
                    .Find(filter)
                    .SortBy(s => s.Date)
                    .ThenBy(s => s.TeamId)
                    .ToListAsync(cancellationTokenSource.Token);

                Console.WriteLine($"[GetStandingsProgressionForYearAsync] Found {standings.Count} progression records for year {year}");
                return standings;
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"[GetStandingsProgressionForYearAsync] Timeout for year {year}: {ex.Message}");
                return new List<Standings>();
            }
            catch (MongoException ex)
            {
                Console.WriteLine($"[GetStandingsProgressionForYearAsync] MongoDB error for year {year}: {ex.Message}");
                return new List<Standings>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetStandingsProgressionForYearAsync] Error for year {year}: {ex.Message}");
                return new List<Standings>();
            }
        }

        // Get the last updated timestamp for cache validation - direct query, no existence checks
        public async Task<DateTime?> GetStandingsLastUpdatedAsync(string year)
        {
            try
            {
                Console.WriteLine($"[GetStandingsLastUpdatedAsync] Direct query for last updated timestamp for year {year}");
                
                var collection = db.Standings[year];
                var filter = Builders<Standings>.Filter.Eq("Year", year);
                
                // Direct query with very short timeout - fail fast if collection doesn't exist
                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                
                var latestRecord = await collection
                    .Find(filter)
                    .SortByDescending(s => s.LastUpdatedAt)
                    .Limit(1)
                    .FirstOrDefaultAsync(cancellationTokenSource.Token);

                var lastUpdated = latestRecord?.LastUpdatedAt;
                Console.WriteLine($"[GetStandingsLastUpdatedAsync] Last updated timestamp for year {year}: {lastUpdated}");
                
                return lastUpdated;
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"[GetStandingsLastUpdatedAsync] Timeout for year {year}: {ex.Message}");
                return null;
            }
            catch (MongoException ex)
            {
                Console.WriteLine($"[GetStandingsLastUpdatedAsync] MongoDB error (collection likely doesn't exist) for year {year}: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetStandingsLastUpdatedAsync] Error for year {year}: {ex.Message}");
                return null;
            }
        }

        // Fast database existence check using MongoDB metadata
        public async Task<bool> DatabaseExistsAsync(string year)
        {
            try
            {
                Console.WriteLine($"[DatabaseExistsAsync] Checking database existence for year {year}");
                
                // Use MongoDB client to list databases - this is a fast metadata operation
                var databaseNames = await db.client.ListDatabaseNamesAsync();
                var databaseList = await databaseNames.ToListAsync();
                
                string expectedDbName = $"wfbc{year}";
                bool exists = databaseList.Contains(expectedDbName);
                
                Console.WriteLine($"[DatabaseExistsAsync] Database {expectedDbName} exists: {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DatabaseExistsAsync] Error checking database for year {year}: {ex.Message}");
                return false;
            }
        }

        // Fast collection existence check using document count instead of problematic metadata operations
        public async Task<bool> CollectionExistsAsync(string year)
        {
            try
            {
                var startTime = DateTime.UtcNow;
                Console.WriteLine($"[CollectionExistsAsync] Using fast document count check for wfbc{year} at {startTime:HH:mm:ss.fff}");
                
                var collection = db.Standings[year];
                
                // Use EstimatedDocumentCountAsync with short timeout - this is a query operation, not metadata
                using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                
                var options = new EstimatedDocumentCountOptions();
                var count = await collection.EstimatedDocumentCountAsync(options, cancellationTokenSource.Token);
                
                var endTime = DateTime.UtcNow;
                var duration = endTime - startTime;
                
                Console.WriteLine($"[CollectionExistsAsync] Document count check completed in {duration.TotalMilliseconds}ms");
                Console.WriteLine($"[CollectionExistsAsync] Collection wfbc{year}.standings exists with ~{count} documents");
                
                return true; // If we got here without exception, collection exists
            }
            catch (TimeoutException ex)
            {
                Console.WriteLine($"[CollectionExistsAsync] Timeout checking collection for year {year}: {ex.Message}");
                return false;
            }
            catch (MongoException ex)
            {
                Console.WriteLine($"[CollectionExistsAsync] MongoDB error (collection likely doesn't exist) for year {year}: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CollectionExistsAsync] Error checking collection for year {year}: {ex.GetType().Name} - {ex.Message}");
                return false;
            }
        }
    }
}
