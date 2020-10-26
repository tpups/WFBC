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
    [Route("api/[controller]")]
    public class WfbcController : Controller
    {
        private readonly IWfbc wfbc;
        public WfbcController(IWfbc _wfbc)
        {
            wfbc = _wfbc;
        }

        [HttpGet]
        public IEnumerable<Manager> Get()
        {
            return wfbc.GetAllManagers();
        }
        [HttpPost]
        public void Post([FromBody] Manager manager)
        {
            wfbc.AddManager(manager);
        }
        [HttpPut]
        public void Put([FromBody]Manager manager)
        {
            wfbc.UpdateManager(manager);
        }
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            wfbc.DeleteManager(id);
        }
        [HttpGet]
        public Standings StandingsByDate(string date)
        {
            DateTime _date = DateTime.Parse(date);
            return wfbc.GetStandingsByDate(_date);
        }
    }
}
