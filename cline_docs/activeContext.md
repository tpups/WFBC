# Active Context

## Current Work
Box score import and 2026 season functionality is fully working. Standings calculate correctly with data imported via the .NET app. All commish tools operational.

## Recent Changes (numbered in order)

1. Added `LeagueId` field to `SeasonSettings` model
2. Updated `SeasonTeam` model — added `ManagerId`, `Year` fields
3. Updated `Manager` model — changed single `TeamId` to `TeamIds` (Dictionary<string, string>)
4. Created `CommishSettings` model for storing Rotowire cookie
5. Created `BoxScoreImport` model with `BoxScoreEntry`, `BoxScoreImportRequest`, `BoxScoreImportResult`
6. Created server interfaces: `ISeasonTeam`, `ICommishSettings`, `IBoxScore`
7. Created server DALs: `SeasonTeamDataAccessLayer`, `CommishSettingsDataAccessLayer`, `BoxScoreDataAccessLayer`
8. Created `RotowireFetchService` — fetches box scores from Rotowire with full browser fingerprint headers, SignalR progress
9. Updated `WfbcDBContext` — added `SeasonTeams`, `BoxScores` (untyped), `BoxScoresTyped` (typed), `CommishSettings`
10. Created server controllers: `SeasonTeamController`, `CommishSettingsController`, `BoxScoreController`
11. Updated `Startup.cs` — registered all new services, `HttpClient("rotowire")` with auto-decompression
12. Rewrote client Teams pages — year-based routing, queries `api/seasonteam/{year}`
13. Rewrote client Managers pages — updated to show `TeamIds` dictionary
14. Created `CommishSettings.razor` — page for commish to paste/save Rotowire cookie
15. Created `UpdateBoxScores.razor` — page with year selector, progress bars, SignalR integration
16. Updated `Commish.razor` — added navigation buttons for new tools
17. Created `SeasonSettingsManager.razor` — commish page for managing season settings per year
18. Fixed `[BsonIgnoreExtraElements]` on `Manager` model for backwards compatibility
19. Updated `appsettings.json` EndYear to 2026
20. **Fixed SignalR auth** — Server reads token from query string for `/progressHub`, client passes `AccessTokenProvider`
21. **Fixed gzip decompression** — Configured `HttpClientHandler` with `AutomaticDecompression` for gzip/deflate/brotli; removed manual `Accept-Encoding` header from `RotowireFetchService`
22. **Updated 2026 nav links** — `NavMenu.razor` results loop starts at 2026; `MainLayout.razor` default Results link → `/results/2026`
23. **Fixed standings ConvertToInt** — Added `long`, `double`, `decimal`, `short` handling to `ConvertToInt()` in `RotisserieStandingsService.cs` (the .NET importer stores numbers as Int64 via `System.Text.Json`, while Python stored Int32)

## Build Status
✅ Build succeeded

## Runtime Status
✅ Standings display working with all stat categories
✅ Box score import from Rotowire working
✅ 2026 season fully operational (teams, settings, box scores, standings)
✅ SignalR progress updates working for both box score import and standings build

## Architecture Notes

### Data Sources
- `wfbc.managers` — Manager documents
- `wfbc.settings` — SeasonSettings documents (one per year; contains `leagueId`, `seasonStartDate`, `seasonEndDate`)
- `wfbc.commish_settings` — single document with Rotowire cookie
- `wfbc{year}.teams` — SeasonTeam documents
- `wfbc{year}.team_box_hitting` / `team_box_pitching` — box score data
- `wfbc{year}.standings` / `compiled_standings` — pre-computed standings

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