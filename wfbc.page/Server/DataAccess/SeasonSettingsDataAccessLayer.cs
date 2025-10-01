using System;
using System.Collections.Generic;
using System.Linq;
using WFBC.Shared.Models;
using WFBC.Server.Models;
using WFBC.Server.Interface;
using MongoDB.Driver;
using MongoDB.Bson;

namespace WFBC.Server.DataAccess
{
    public class SeasonSettingsDataAccessLayer : ISeasonSettings
    {
        private WfbcDBContext _db;
        public SeasonSettingsDataAccessLayer(WfbcDBContext db)
        {
            _db = db;
        }

        // Get all Season Settings
        public List<SeasonSettings> GetAllSeasonSettings()
        {
            try
            {
                return _db.Settings.Find(_ => true).ToList();
            }
            catch
            {
                throw;
            }
        }

        // Get season settings for a specific year
        public SeasonSettings GetSeasonSettings(int year)
        {
            try
            {
                FilterDefinition<SeasonSettings> settingsData = Builders<SeasonSettings>.Filter.Eq("Year", year);
                return _db.Settings.Find(settingsData).FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }

        // Add season settings
        public void AddSeasonSettings(SeasonSettings seasonSettings)
        {
            try
            {
                // Generate a new ObjectId for new documents
                seasonSettings.Id = ObjectId.GenerateNewId().ToString();
                seasonSettings.CreatedAt = DateTime.UtcNow;
                seasonSettings.UpdatedAt = DateTime.UtcNow;
                _db.Settings.InsertOne(seasonSettings);
            }
            catch
            {
                throw;
            }
        }

        // Update season settings
        public void UpdateSeasonSettings(SeasonSettings seasonSettings)
        {
            try
            {
                seasonSettings.UpdatedAt = DateTime.UtcNow;
                
                // Check if this is an existing document or a new one
                var existingSettings = GetSeasonSettings(seasonSettings.Year);
                
                if (existingSettings != null)
                {
                    // Update existing document - preserve the existing Id
                    seasonSettings.Id = existingSettings.Id;
                    seasonSettings.CreatedAt = existingSettings.CreatedAt; // Preserve original creation date
                    _db.Settings.ReplaceOne(filter: s => s.Year == seasonSettings.Year, replacement: seasonSettings);
                }
                else
                {
                    // Create new document - generate a new ObjectId
                    seasonSettings.Id = ObjectId.GenerateNewId().ToString();
                    seasonSettings.CreatedAt = DateTime.UtcNow;
                    _db.Settings.ReplaceOne(filter: s => s.Year == seasonSettings.Year, replacement: seasonSettings, options: new ReplaceOptions { IsUpsert = true });
                }
            }
            catch
            {
                throw;
            }
        }

        // Delete season settings for a specific year
        public void DeleteSeasonSettings(int year)
        {
            try
            {
                FilterDefinition<SeasonSettings> settingsData = Builders<SeasonSettings>.Filter.Eq("Year", year);
                _db.Settings.DeleteOne(settingsData);
            }
            catch
            {
                throw;
            }
        }
    }
}
