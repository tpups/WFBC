using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using WFBC.Server.Hubs;
using WFBC.Server.Interface;
using WFBC.Shared.Models;

namespace WFBC.Server.Services
{
    public class RotowireFetchProgress
    {
        public int TotalTeams { get; set; }
        public int CurrentTeam { get; set; }
        public string? TeamName { get; set; }
        public int TotalDays { get; set; }
        public int CurrentDay { get; set; }
        public string? CurrentDate { get; set; }
        public string? Status { get; set; }
        public int NewEntries { get; set; }
        public int UpdatedEntries { get; set; }
    }

    public class RotowireFetchService
    {
        private readonly IHubContext<ProgressHub> _hub;
        private readonly IBoxScore _boxScore;
        private readonly ISeasonSettings _seasonSettings;
        private readonly ISeasonTeam _seasonTeam;
        private readonly IHttpClientFactory _http;

        public RotowireFetchService(IHubContext<ProgressHub> hub, IBoxScore boxScore, ISeasonSettings seasonSettings, ISeasonTeam seasonTeam, IHttpClientFactory http)
        {
            _hub = hub; _boxScore = boxScore; _seasonSettings = seasonSettings; _seasonTeam = seasonTeam; _http = http;
        }

        public async Task FetchBoxScores(string year, string cookie, string connectionId, CancellationToken ct = default)
        {
            var settings = _seasonSettings.GetSeasonSettings(int.Parse(year));
            if (settings == null) { await Progress(connectionId, "Error: No season settings for " + year, 0, 0, 0, 0); return; }
            var teams = _seasonTeam.GetTeamsForYear(year);
            if (teams == null || teams.Count == 0) { await Progress(connectionId, "Error: No teams for " + year, 0, 0, 0, 0); return; }
            if (string.IsNullOrEmpty(settings.LeagueId)) { await Progress(connectionId, "Error: No league ID for " + year, 0, 0, 0, 0); return; }

            var start = settings.SeasonStartDate.Date;
            var end = settings.SeasonEndDate.Date;
            if (DateTime.UtcNow.Date < end) end = DateTime.UtcNow.Date;
            int totalDays = (int)(end - start).TotalDays + 1;
            var shortYear = year.Substring(2);
            var baseUrl = $"https://www.rotowire.com/mlbcommish{shortYear}/tables/box.php";
            var client = _http.CreateClient("rotowire");
            int totalNew = 0, totalUpd = 0;

            for (int ti = 0; ti < teams.Count && !ct.IsCancellationRequested; ti++)
            {
                var team = teams[ti];
                var cur = start;
                int di = 0;
                while (cur <= end && !ct.IsCancellationRequested)
                {
                    var ds = cur.ToString("yyyy-MM-dd");
                    await _hub.Clients.Client(connectionId).SendAsync("ReceiveProgress", new RotowireFetchProgress
                    { TotalTeams = teams.Count, CurrentTeam = ti + 1, TeamName = team.TeamName ?? team.Manager, TotalDays = totalDays, CurrentDay = di + 1, CurrentDate = ds, Status = "Fetching", NewEntries = totalNew, UpdatedEntries = totalUpd }, ct);

                    var hitData = await Fetch(client, baseUrl, settings.LeagueId, team.TeamId!, ds, "B", cookie);
                    if (hitData != null) { foreach (var e in hitData) { e["teamID"] = team.TeamId; e["stats_date"] = ds; e["download_date"] = DateTime.UtcNow.ToString("o"); } var r = await _boxScore.ImportBoxScores(year, ToEntries(hitData), "hitting"); totalNew += r.NewEntries; totalUpd += r.UpdatedEntries; }
                    await Task.Delay(100, ct);

                    var pitchData = await Fetch(client, baseUrl, settings.LeagueId, team.TeamId!, ds, "P", cookie);
                    if (pitchData != null) { foreach (var e in pitchData) { e["teamID"] = team.TeamId; e["stats_date"] = ds; e["download_date"] = DateTime.UtcNow.ToString("o"); } var r = await _boxScore.ImportBoxScores(year, ToEntries(pitchData), "pitching"); totalNew += r.NewEntries; totalUpd += r.UpdatedEntries; }
                    await Task.Delay(100, ct);

                    cur = cur.AddDays(1); di++;
                }
            }
            await _hub.Clients.Client(connectionId).SendAsync("ReceiveProgress", new RotowireFetchProgress
            { TotalTeams = teams.Count, CurrentTeam = teams.Count, TotalDays = totalDays, CurrentDay = totalDays, Status = "Complete", NewEntries = totalNew, UpdatedEntries = totalUpd }, ct);
        }

        private async Task<List<Dictionary<string, object?>>?> Fetch(HttpClient client, string baseUrl, string leagueId, string teamId, string date, string borp, string cookie)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}?leagueID={leagueId}&teamID={teamId}&date={date}&borp={borp}");
                req.Headers.Add("Cookie", cookie);
                req.Headers.Add("Host", "www.rotowire.com");
                req.Headers.Add("Connection", "keep-alive");
                req.Headers.Add("Cache-Control", "max-age=0");
                req.Headers.Add("DNT", "1");
                req.Headers.Add("Upgrade-Insecure-Requests", "1");
                req.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.105 Safari/537.36 Edg/84.0.522.52");
                req.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
                req.Headers.Add("Sec-Fetch-Site", "none");
                req.Headers.Add("Sec-Fetch-Mode", "navigate");
                req.Headers.Add("Sec-Fetch-User", "?1");
                req.Headers.Add("Sec-Fetch-Dest", "document");
                req.Headers.Add("Accept-Language", "en-US,en;q=0.9,mt;q=0.8");
                var resp = await client.SendAsync(req);
                if (!resp.IsSuccessStatusCode) return null;
                var content = await resp.Content.ReadAsStringAsync();
                if (string.IsNullOrWhiteSpace(content) || content.TrimStart().StartsWith("<")) return null;
                return JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }

        private List<BoxScoreEntry> ToEntries(List<Dictionary<string, object?>> raw)
        {
            var list = new List<BoxScoreEntry>();
            foreach (var d in raw) { var e = new BoxScoreEntry(); foreach (var kv in d) e[kv.Key] = kv.Value; list.Add(e); }
            return list;
        }

        private Task Progress(string cid, string status, int tt, int ct2, int td, int cd)
            => _hub.Clients.Client(cid).SendAsync("ReceiveProgress", new RotowireFetchProgress { Status = status, TotalTeams = tt, CurrentTeam = ct2, TotalDays = td, CurrentDay = cd });
    }
}