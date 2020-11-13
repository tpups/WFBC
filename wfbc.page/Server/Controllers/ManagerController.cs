using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using wfbc.page.Server.DataAccess;
using wfbc.page.Shared.Models;
using wfbc.page.Server.Interface;
using Okta.AspNetCore;

namespace wfbc.page.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ManagerController : ControllerBase
    {
        private readonly IManager _manager;
        public ManagerController(IManager manager)
        {
            _manager = manager;
        }

        [HttpGet]
        public List<Manager> Get()
        {
            return _manager.GetAllManagers();
        }
        [HttpGet("{id}")]
        public Manager Get(string id)
        {
            return _manager.GetManager(id);
        }
        [HttpPost]
        public void Post([FromBody] Manager manager)
        {
            _manager.AddManager(manager);
        }
        [HttpPut]
        public void Put([FromBody] Manager manager)
        {
            _manager.UpdateManager(manager);
        }
        [Authorize(Policy = Policies.IsCommish)]
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            _manager.DeleteManager(id);
        }
    }
}
