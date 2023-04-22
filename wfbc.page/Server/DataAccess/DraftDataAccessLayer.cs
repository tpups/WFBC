using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using WFBC.Server.Models;
using WFBC.Server.Interface;
using MongoDB.Driver;
using System.Net.Http;
using WFBC.Client;

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
        public string AddDraft(Draft draft)
        {
            try
            {
                _db.Drafts.InsertOne(draft);
                return draft.Id;
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
                FilterDefinitionBuilder<Pick> picksData = new FilterDefinitionBuilder<Pick>();
                FilterDefinition<Pick> picksFilter = draftPicks != null && draftPicks.Any() ? picksData.In(p => p.Id, draftPicks) : null;
                if (picksFilter != null)
                {
                    _db.Picks.DeleteMany(picksFilter);
                }
                _db.Drafts.DeleteOne(draftData);
            }
            catch
            {
                throw;
            }
        }
    }
}
