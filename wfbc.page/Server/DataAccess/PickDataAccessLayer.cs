﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WFBC.Shared.Models;
using WFBC.Server.Models;
using WFBC.Server.Interface;
using MongoDB.Driver;

namespace WFBC.Server.DataAccess
{
    public class PickDataAccessLayer : IPick
    {
        private WfbcDBContext _db;
        public PickDataAccessLayer(WfbcDBContext db)
        {
            _db = db;
        }

        public List<Pick> GetPicks(List<string> picks)
        {
            try
            {
                var picksData = new FilterDefinitionBuilder<Pick>();
                var picksFilter = picksData.In(p => p.Id, picks);
                return _db.Picks.Find(picksFilter).ToList();
            }
            catch
            {
                throw;
            }
        }
        public Pick GetPick(string id)
        {
            try
            {
                FilterDefinition<Pick> pickData = Builders<Pick>.Filter.Eq("Id", id);
                return _db.Picks.Find(pickData).FirstOrDefault();
            }
            catch
            {
                throw;
            }
        }
        // Add a draft pick
        public string AddPick(Pick pick)
        {
            try
            {
                _db.Picks.InsertOne(pick);
                return pick.Id;
            }
            catch
            {
                throw;
            }
        }
        // Add a list of draft picks
        public async Task<string[]> AddPicks(List<Pick> picks)
        {
            try
            {
                // Insert picks one by one to get the generated IDs
                var pickIDs = new List<string>();
                foreach (var pick in picks)
                {
                    await _db.Picks.InsertOneAsync(pick);
                    pickIDs.Add(pick.Id);
                }
                return pickIDs.ToArray();
            }
            catch
            {
                throw;
            }
        }
        // Update a draft pick
        public void UpdatePick(Pick pick)
        {
            try
            {
                _db.Picks.ReplaceOne(filter: p => p.Id == pick.Id, replacement: pick);
            }
            catch
            {
                throw;
            }
        }
        // Delete a draft pick
        public void DeletePick(string id)
        {
            try
            {
                FilterDefinition<Pick> pickData = Builders<Pick>.Filter.Eq("Id", id);
                _db.Picks.DeleteOne(pickData);
            }
            catch
            {
                throw;
            }
        }
        // Delete multiple draft picks
        public void DeletePicks(string[] ids)
        {
            try
            {
                FilterDefinition<Pick> pickData = Builders<Pick>.Filter.In("Id", ids);
                _db.Picks.DeleteMany(pickData);
            }
            catch
            {
                throw;
            }
        }
    }
}
