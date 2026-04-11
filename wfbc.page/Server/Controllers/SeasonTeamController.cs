using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WFBC.Server.Interface;
using WFBC.Shared.Models;

namespace WFBC.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeasonTeamController : ControllerBase
    {
        private readonly ISeasonTeam _seasonTeam;
        public SeasonTeamController(ISeasonTeam seasonTeam) { _seasonTeam = seasonTeam; }

        [HttpGet("{year}")]
        public List<SeasonTeam> Get(string year) => _seasonTeam.GetTeamsForYear(year);

        [HttpGet("{year}/{id}")]
        public SeasonTeam GetOne(string year, string id) => _seasonTeam.GetSeasonTeam(year, id);

        [HttpPost("{year}")]
        [Authorize(Policy = Policies.IsCommish)]
        public void Post(string year, [FromBody] SeasonTeam team) => _seasonTeam.AddSeasonTeam(year, team);

        [HttpPut("{year}")]
        [Authorize(Policy = Policies.IsCommish)]
        public void Put(string year, [FromBody] SeasonTeam team) => _seasonTeam.UpdateSeasonTeam(year, team);

        [HttpDelete("{year}/{id}")]
        [Authorize(Policy = Policies.IsCommish)]
        public void Delete(string year, string id) => _seasonTeam.DeleteSeasonTeam(year, id);
    }
}