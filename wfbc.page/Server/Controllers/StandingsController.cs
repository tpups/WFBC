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
