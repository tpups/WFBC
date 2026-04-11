using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WFBC.Server.Interface;
using WFBC.Server.Services;
using WFBC.Shared.Models;

namespace WFBC.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BoxScoreController : ControllerBase
    {
        private readonly IBoxScore _boxScore;
        private readonly ICommishSettings _commishSettings;
        private readonly RotowireFetchService _fetchService;

        public BoxScoreController(IBoxScore boxScore, ICommishSettings commishSettings, RotowireFetchService fetchService)
        { _boxScore = boxScore; _commishSettings = commishSettings; _fetchService = fetchService; }

        [HttpPost("fetch/{year}")]
        [Authorize(Policy = Policies.IsCommish)]
        public async Task<IActionResult> FetchBoxScores(string year, [FromQuery] string connectionId)
        {
            var settings = _commishSettings.GetCommishSettings();
            if (settings == null || string.IsNullOrEmpty(settings.RotowireCookie))
                return BadRequest("No Rotowire cookie configured. Please set it in Settings.");
            _ = Task.Run(() => _fetchService.FetchBoxScores(year, settings.RotowireCookie, connectionId, CancellationToken.None));
            return Ok(new { message = "Box score fetch started" });
        }

        [HttpPost("import")]
        [Authorize(Policy = Policies.IsCommish)]
        public async Task<BoxScoreImportResult> ImportBoxScores([FromBody] BoxScoreImportRequest request)
        {
            var entries = new List<BoxScoreEntry>();
            foreach (var d in request.Entries) { var e = new BoxScoreEntry(); foreach (var kv in d) e[kv.Key] = kv.Value; entries.Add(e); }
            return await _boxScore.ImportBoxScores(request.Year, entries, request.Type);
        }
    }
}