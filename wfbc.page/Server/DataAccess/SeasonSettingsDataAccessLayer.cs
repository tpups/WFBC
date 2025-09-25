using System;
using System.Collections.Generic;
using System.Linq;
using WFBC.Shared.Models;
using WFBC.Server.Models;
using WFBC.Server.Interface;
using MongoDB.Driver;

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
                _db.Settings.ReplaceOne(filter: s => s.Year == seasonSettings.Year, replacement: seasonSettings);
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
