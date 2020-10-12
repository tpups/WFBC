using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using wfbc.page.Server.DataAccess;
using wfbc.page.Shared.Models;

namespace wfbc.page.Server.Controllers
{
    public class WfbcController : Controller
    {
        wfbcDataAccessLayer WfbcData = new wfbcDataAccessLayer();
        
        [HttpGet]
        [Route("api/Standings/StandingsByDate/{date}")]
        public Standings StandingsByDate(string date)
        {
            DateTime _date = DateTime.Parse(date);
            return WfbcData.GetStandingsByDate(_date);
        }
    }
}
