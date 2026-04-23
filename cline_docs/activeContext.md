# Active Context

## Current Work
Improved auth performance and fixed token expiry bug causing "SignalR not connected" errors.

## Recent Changes (numbered in order)

1-33. (Previous changes — see git history)
34. **Fixed GS stuck in advanced stats** (`BoxScoreDataAccessLayer.cs`) — Added `"GS"` to the `PitchingCats` array so that Games Started values get updated when pitching box scores are re-imported for the same date.
35. **Auth performance improvements** (`Startup.cs`) — Replaced `new HttpClient()` with `IHttpClientFactory` (named "zitadel" client) for userinfo endpoint calls to reuse connections. Increased role cache duration from 5 to 30 minutes.
36. **Fixed token expiry / SignalR bug** (`UpdateBoxScores.razor`, `BuildUpdateStandings.razor.cs`) — Added `EnsureHubConnected()` method that checks token freshness and rebuilds SignalR connection if expired. Added `.WithAutomaticReconnect()` to `BuildUpdateStandings`. Both pages now detect 401 responses and show "Your session has expired" instead of generic errors.
37. **Faster logout** (`MenuBar.razor`) — Added `post_logout_redirect_uri` (Navigation.BaseUri) to logout call so user is redirected back to home after Zitadel logout instead of lingering on Zitadel's page.

## Build Status
✅ Build succeeded (verified locally)

## Runtime Status
⏳ Production deployment pending

## Architecture Notes

### Chart Width Calculation (StandingsGraph.razor)
- **Mobile (<640px)**: Uses `100vw`-based calculations (drawer doesn't affect mobile)
- **Desktop**: `document.documentElement.clientWidth - (drawerMargin + 20px scrollbar buffer)`
  - Drawer closed: 148px total margin
  - Drawer minified: 276px total margin
  - Drawer open: 404px total margin
- **Tablet**: `document.documentElement.clientWidth - 68px`
- Key insight: `100vw` includes browser scrollbar width (~17px on Windows), `document.documentElement.clientWidth` does not
- Additional 20px buffer accounts for `<main>` element's own vertical scrollbar
- Cannot use `parentElement.clientWidth` — fails when navigating from hidden tabs (Advanced → Chart)
- Cannot use CSS-only sizing — Chart.js `responsive: true` creates feedback loop without explicit width constraints

### Advanced Stats Design
- `AdvancedStats` model intentionally separated from scoring categories on `Standings`
- All current stats derived from active-lineup totals (position == "A", player == "TOT")
- Future inactive-player stats can be added to `AdvancedStats` without disturbing scoring logic
- `PopulateAdvancedStats` called in both `CalculateStandingsForDate` and `ProcessIncrementalStandings`
- Older compiled standings documents won't have `advanced` field until standings are rebuilt for that year
- Client `AdvancedStandings.razor` uses declarative `AdvancedCategory` records — add new entries to extend

### Data Sources
- `wfbc.managers` — Manager documents
- `wfbc.settings` — SeasonSettings documents (one per year; contains `leagueId`, `seasonStartDate`, `seasonEndDate`)
- `wfbc.commish_settings` — single document with Rotowire cookie
- `wfbc{year}.teams` — SeasonTeam documents
- `wfbc{year}.team_box_hitting` / `team_box_pitching` — box score data
- `wfbc{year}.standings` / `compiled_standings` — pre-computed standings (now includes `advanced` sub-document)

### Key Technical Details
- Rotowire HTTP client uses `AutomaticDecompression` (gzip/deflate/brotli) configured via `ConfigurePrimaryHttpMessageHandler` in `Startup.cs`
- Box score numeric values stored as `Int64` (long) in MongoDB via `System.Text.Json` → `TryGetInt64`
- `ConvertToInt()` in standings service handles `int`, `long`, `double`, `decimal`, `short`, and `string` types
- SignalR `/progressHub` auth uses query string `access_token` parameter (WebSocket can't send Authorization headers)
- `ResultsDynamic.razor` handles years 2019+ with parameterized route `/results/{year:int}`

## Next Steps
- Deploy updated container to production and re-import box scores to pick up missing GS values
- Re-run standings calculation after re-import to rebuild with correct GS totals
- Test auth improvements in production (verify faster login, proper session expiry handling)
- Consider automating box score imports (scheduled task or cron)
- Trophy.razor needs 2026 entry added at end of season
- Future: add more advanced stat categories (e.g. FIP, wOBA)
- Future: include inactive-lineup stats in AdvancedStats
