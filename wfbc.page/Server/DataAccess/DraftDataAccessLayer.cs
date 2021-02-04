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
    public class DraftDataAccessLayer : IDraft
    {
        private WfbcDBContext _db;
        public DraftDataAccessLayer(WfbcDBContext db)
        {
            _db = db;
        }

        // Get all managers
        public List<Draft> GetAllDrafts()
        {
            try
            {
                return _db.Drafts.Find(_ => true).ToList();
            }
            catch
            {
                throw;
            }
        }
        // Get one manger
        public Draft GetDraft(string id)
        {
            try
            {
                FilterDefinition<Draft> draftData = Builders<Draft>.Filter.Eq("Id", id);
                return _db.Drafts.Find(draftData).FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }
        // Add a draft
        public void AddDraft(Draft draft)
        {
            try
            {
                _db.Drafts.InsertOne(draft);
            }
            catch
            {
                throw;
            }
        }
        // Update a draft
        public void UpdateDraft(Draft draft)
        {
            try
            {
                _db.Drafts.ReplaceOne(filter: p => p.Id == draft.Id, replacement: draft);
            }
            catch
            {
                throw;
            }
        }
        // Delete a draft
        public void DeleteDraft(string id)
        {
            try
            {
                FilterDefinition<Draft> draftData = Builders<Draft>.Filter.Eq("Id", id);
                // also delete all picks for the draft
                List<string> draftPicks = GetDraft(id).Picks;
                var picksData = new FilterDefinitionBuilder<Pick>();
                var picksFilter = picksData.In(p => p.Id, draftPicks);
                _db.Drafts.DeleteOne(draftData);
                _db.Picks.DeleteMany(picksFilter);
            }
            catch
            {
                throw;
            }
        }
    }
}
