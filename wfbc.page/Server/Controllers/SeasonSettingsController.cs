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
    public class SeasonSettingsController : ControllerBase
    {
        private readonly ISeasonSettings _seasonSettings;
        public SeasonSettingsController(ISeasonSettings seasonSettings)
        {
            _seasonSettings = seasonSettings;
        }

        [HttpGet]
        public List<SeasonSettings> Get()
        {
            return _seasonSettings.GetAllSeasonSettings();
        }

        [HttpGet("{year}")]
        public SeasonSettings Get(int year)
        {
            return _seasonSettings.GetSeasonSettings(year);
        }

        [HttpPost]
        [Authorize(Policy = Policies.IsCommish)]
        public void Post([FromBody] SeasonSettings seasonSettings)
        {
            _seasonSettings.AddSeasonSettings(seasonSettings);
        }

        [HttpPut]
        [Authorize(Policy = Policies.IsCommish)]
        public void Put([FromBody] SeasonSettings seasonSettings)
        {
            _seasonSettings.UpdateSeasonSettings(seasonSettings);
        }

        [HttpDelete("{year}")]
        [Authorize(Policy = Policies.IsCommish)]
        public void Delete(int year)
        {
            _seasonSettings.DeleteSeasonSettings(year);
        }
    }
}
