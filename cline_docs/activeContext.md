# Active Context

## Current Work
Advanced Stats tab added to results pages. Shows leaderboard cards for Starts (GS), Quality Start Rate (QS/GS), Strikeout Rate (K/PA), and Walk Rate (BB/PA). All four categories working correctly after fixing missing K mapping in GetHittingStatValue.

## Recent Changes (numbered in order)

1-23. (Previous changes — see git history)
24. **Created `AdvancedStats` model** (`wfbc.page/Shared/Models/AdvancedStats.cs`) — Separate bucket for advanced stats: Starts, QualityStartRate, StrikeoutRateBatting, WalkRateBatting, plus raw components (BattingK, BattingBB, BattingPA, QualityStarts)
25. **Added `Advanced` property to `Standings` model** — `[BsonIgnoreIfNull]` nullable `AdvancedStats?` field, backwards-compatible with older compiled docs
26. **Extended `RotisserieStandingsService`** — Added `GS` to pitching stat arrays, `K` to hitting stat arrays, `GS` mapping in `GetPitchingStatValue`, `K` mapping in `GetHittingStatValue`, new `PopulateAdvancedStats` method called after every `BuildStandingsFromPoints`
27. **Created `AdvancedStandings.razor`** — Card-grid component with declarative category configuration; each card shows ranked leaderboard with team name, manager, formatted stat value; gold/silver/bronze row highlights for top 3
28. **Updated `StandingsDisplay.razor`** — Added third "Advanced" tab between Standings Table and Points Over Time; reuses `finalStandings` data (no new API calls); handles tab switching and loading state

## Build Status
✅ Build succeeded

## Runtime Status
✅ All four advanced stat cards displaying correctly
✅ Standings Table and Points Over Time tabs unaffected
✅ Advanced tab reuses existing final standings data — no additional API calls

## Architecture Notes

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
- Future: add more advanced stat categories (e.g. BABIP, FIP, wOBA)
- Future: include inactive-lineup stats in AdvancedStats