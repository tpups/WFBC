# Active Context

## Current Work
Fixed GS (Games Started) advanced stat not updating on box score re-imports due to missing field in update array.

## Recent Changes (numbered in order)

1-33. (Previous changes — see git history)
34. **Fixed GS stuck in advanced stats** (`BoxScoreDataAccessLayer.cs`) — Added `"GS"` to the `PitchingCats` array so that Games Started values get updated when pitching box scores are re-imported for the same date. Previously, GS was missing from this array, meaning re-imports silently skipped GS changes while all other pitching stats were properly updated.

## Build Status
✅ Build succeeded (verified locally)

## Runtime Status
✅ GS updates correctly on local instance after fix
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
- Consider automating box score imports (scheduled task or cron)
- Trophy.razor needs 2026 entry added at end of season
- Future: add more advanced stat categories (e.g. FIP, wOBA)
- Future: include inactive-lineup stats in AdvancedStats
