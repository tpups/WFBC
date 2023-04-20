using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WFBC.Server.DataAccess;
using WFBC.Shared.Models;
using WFBC.Server.Interface;


namespace WFBC.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeamController : ControllerBase
    {
        private readonly ITeam _team;
        public TeamController(ITeam team)
        {
            _team = team;
        }

        [HttpGet]
        public List<Team> Get()
        {
            return _team.GetAllTeams();
        }
        [HttpGet("{id}")]
        public Team Get(string id)
        {
            return _team.GetTeam(id);
        }
        [HttpPost]
        [Authorize(Policy = Policies.IsCommish)]
        public void Post([FromBody] Team team)
        {
            _team.AddTeam(team);
        }
        [HttpPut]
        [Authorize(Policy = Policies.IsCommish)]
        public void Put([FromBody] Team team)
        {
            _team.UpdateTeam(team);
        }
        [HttpDelete("{id}")]
        [Authorize(Policy = Policies.IsCommish)]
        public void Delete(string id)
        {
            _team.DeleteTeam(id);
        }
    }
}
