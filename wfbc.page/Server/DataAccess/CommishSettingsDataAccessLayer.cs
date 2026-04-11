using System;
using WFBC.Shared.Models;
using WFBC.Server.Models;
using WFBC.Server.Interface;
using MongoDB.Driver;
using MongoDB.Bson;

namespace WFBC.Server.DataAccess
{
    public class CommishSettingsDataAccessLayer : ICommishSettings
    {
        private readonly WfbcDBContext _db;
        public CommishSettingsDataAccessLayer(WfbcDBContext db) { _db = db; }

        public CommishSettings GetCommishSettings()
            => _db.CommishSettings.Find(_ => true).FirstOrDefault();

        public void SaveCommishSettings(CommishSettings settings)
        {
            settings.UpdatedAt = DateTime.UtcNow;
            var existing = GetCommishSettings();
            if (existing != null)
            {
                settings.Id = existing.Id;
                settings.CreatedAt = existing.CreatedAt;
                _db.CommishSettings.ReplaceOne(s => s.Id == existing.Id, settings);
            }
            else
            {
                settings.Id = ObjectId.GenerateNewId().ToString();
                settings.CreatedAt = DateTime.UtcNow;
                _db.CommishSettings.InsertOne(settings);
            }
        }
    }
}