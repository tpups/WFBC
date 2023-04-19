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
    [Authorize(Policy = Policies.IsCommish)]
    public class TeamController : ControllerBase
    {
        private readonly ITeam _team;
        public TeamController(ITeam team)
        {
            _team = team;
        }

        [HttpGet]
        [AllowAnonymous]
        public List<Team> Get()
        {
            return _team.GetAllTeams();
        }
        [HttpGet("{id}")]
        [AllowAnonymous]
        public Team Get(string id)
        {
            return _team.GetTeam(id);
        }
        [HttpPost]
        public void Post([FromBody] Team team)
        {
            _team.AddTeam(team);
        }
        [HttpPut]
        public void Put([FromBody] Team team)
        {
            _team.UpdateTeam(team);
        }
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            _team.DeleteTeam(id);
        }
    }
}
