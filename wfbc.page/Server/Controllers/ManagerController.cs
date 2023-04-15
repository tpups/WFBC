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
    public class ManagerController : ControllerBase
    {
        private readonly IManager _manager;
        public ManagerController(IManager manager)
        {
            _manager = manager;
        }

        [HttpGet]
        [AllowAnonymous]
        public List<Manager> Get()
        {
            return _manager.GetAllManagers();
        }
        [HttpGet("{id}")]
        [AllowAnonymous]
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
        [HttpDelete("{id}")]
        public void Delete(string id)
        {
            _manager.DeleteManager(id);
        }
    }
}
