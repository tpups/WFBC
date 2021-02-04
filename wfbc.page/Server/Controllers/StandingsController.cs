using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WFBC.Server.DataAccess;
using WFBC.Shared.Models;
using WFBC.Server.Interface;

namespace WFBC.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StandingsController : ControllerBase
    {
        private readonly IStandings wfbc;
        public StandingsController(IStandings _wfbc)
        {
            wfbc = _wfbc;
        }

        [HttpGet]
        public Standings StandingsByDate(string date)
        {
            DateTime _date = DateTime.Parse(date);
            return wfbc.GetStandingsByDate(_date);
        }
    }
}
