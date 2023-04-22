using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WFBC.Server.DataAccess;
using WFBC.Shared.Models;
using WFBC.Server.Interface;
using Microsoft.AspNetCore.Authorization;

namespace WFBC.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = Policies.IsCommish)]
    public class DraftController : ControllerBase
    {
        private readonly IDraft _draft;
        public DraftController(IDraft draft)
        {
            _draft = draft;
        }
        [HttpGet]
        [AllowAnonymous]
        public List<Draft> Get()
        {
            return _draft.GetAllDrafts();
        }
        [HttpGet("{id}")]
        [AllowAnonymous]
        public Draft Get(string id)
        {
            return _draft.GetDraft(id);
        }
        [HttpPost]
        public string Post([FromBody]Draft draft)
        {
            _draft.AddDraft(draft);
            return draft.Id;
        }
        [HttpPut]
        public void Put([FromBody]Draft draft)
        {
            _draft.UpdateDraft(draft);
        }
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            _draft.DeleteDraft(id);
        }
    }
}
