using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WFBC.Server.Interface;
using WFBC.Shared.Models;

namespace WFBC.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommishSettingsController : ControllerBase
    {
        private readonly ICommishSettings _settings;
        public CommishSettingsController(ICommishSettings settings) { _settings = settings; }

        [HttpGet]
        [Authorize(Policy = Policies.IsCommish)]
        public CommishSettings Get() => _settings.GetCommishSettings();

        [HttpPost]
        [Authorize(Policy = Policies.IsCommish)]
        public void Post([FromBody] CommishSettings settings) => _settings.SaveCommishSettings(settings);
    }
}