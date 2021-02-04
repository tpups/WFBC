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
    public class DraftController : ControllerBase
    {
        private readonly IDraft _draft;
        public DraftController(IDraft draft)
        {
            _draft = draft;
        }
        [HttpGet]
        public List<Draft> Get()
        {
            return _draft.GetAllDrafts();
        }
        [HttpGet("{id}")]
        public Draft Get(string id)
        {
            return _draft.GetDraft(id);
        }
        [HttpPost]
        [Authorize(Policy = Policies.IsCommish)]
        public void Post([FromBody]Draft draft)
        {
            _draft.AddDraft(draft);
        }
        [HttpPut]
        [Authorize(Policy = Policies.IsCommish)]
        public void Put([FromBody]Draft draft)
        {
            _draft.UpdateDraft(draft);
        }
        [HttpDelete]
        [Authorize(Policy = Policies.IsCommish)]
        public void Delete(string id)
        {
            _draft.DeleteDraft(id);
        }
    }
}
