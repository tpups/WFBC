# Active Context

## Current Work
Fixed styling regressions introduced when adding the Advanced tab to results pages. Three issues resolved: standings table bottom bar, chart horizontal scrollbar, and advanced cards mobile overflow.

## Recent Changes (numbered in order)

1-30. (Previous changes — see git history)
31. **Fixed Standings Table bottom blue bar** (`StandingsTable.razor`) — Changed `h-[60dvh] sm:h-[70vh]` to `max-h-[60dvh] sm:max-h-[70vh]` on scroll container so table shrinks to fit content instead of maintaining fixed height with blue background showing through
32. **Fixed Points Over Time chart horizontal scrollbar** (`StandingsGraph.razor`) — Replaced `100vw`-based viewport calculations with `document.documentElement.clientWidth` (excludes browser scrollbar) plus 20px buffer for `<main>` element's vertical scrollbar. Drawer-based margins retained: closed=148px, minified=276px, open=404px, tablet=68px. CSS media queries changed to `100%` fallbacks.
33. **Fixed Advanced cards mobile overflow** — CSS `max-w-full` and grid `1fr` column on mobile already handled by existing styles; the `max-h` fix on standings table resolved the container overflow that was pushing cards wide

## Build Status
✅ Build succeeded

## Runtime Status
✅ Standings Table — no dark blue bar on right/bottom on larger screens
✅ Points Over Time — no horizontal scrollbar on larger screens
✅ Advanced tab — cards properly sized on mobile (iPhone 17 Pro 402px)
✅ Tab switching (Advanced → Chart) works correctly
✅ Drawer open/closed/minified states all render chart correctly

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
- Deploy updated container to production
- Consider automating box score imports (scheduled task or cron)
- Trophy.razor needs 2026 entry added at end of season
- Future: add more advanced stat categories (e.g. FIP, wOBA)
- Future: include inactive-lineup stats in AdvancedStats