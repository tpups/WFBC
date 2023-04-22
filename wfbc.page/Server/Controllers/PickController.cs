using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WFBC.Server.DataAccess;
using WFBC.Shared.Models;
using WFBC.Server.Interface;
using Amazon.Auth.AccessControlPolicy;
using Microsoft.AspNetCore.Authorization;

namespace WFBC.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PickController : ControllerBase
    {
        private readonly IPick _pick;
        public PickController(IPick pick)
        {
            _pick = pick;
        }
        [HttpGet]
        public List<Pick> GetPicks(List<string> picks)
        {
            return _pick.GetPicks(picks);
        }
        [HttpGet("{id}")]
        public Pick Get(string id)
        {
            return _pick.GetPick(id);
        }
        [HttpPost]
        [Authorize(Policy = Policies.IsCommish)]
        public string[] Post([FromBody] List<Pick> picks)
        {
            _pick.AddPicks(picks);
            var pickIDs = new List<string>();
            foreach (var pick in picks)
            {
                pickIDs.Add(pick.Id);
            }
            return pickIDs.ToArray();
        }
    }
}
