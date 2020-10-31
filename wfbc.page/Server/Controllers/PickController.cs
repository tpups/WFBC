using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using wfbc.page.Server.DataAccess;
using wfbc.page.Shared.Models;
using wfbc.page.Server.Interface;

namespace wfbc.page.Server.Controllers
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
        public void Post([FromBody] List<Pick> picks)
        {
            _pick.AddPicks(picks);
        }
    }
}
