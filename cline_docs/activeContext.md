# Active Context

## Current Work
Implementing box score import functionality into the .NET Blazor app, migrating functionality from Python scripts in `C:\dev\wfbc_utils`. Build succeeds. Standings and Teams display confirmed working.

## Recent Changes (numbered in order)

1. Added `LeagueId` field to `SeasonSettings` model (`wfbc.page/Shared/Models/SeasonSettings.cs`)
2. Updated `SeasonTeam` model — added `ManagerId`, `Year` fields (`wfbc.page/Shared/Models/SeasonTeam.cs`)
3. Updated `Manager` model — changed single `TeamId` (string) to `TeamIds` (Dictionary<string, string> mapping year → teamId) (`wfbc.page/Shared/Models/Manager.cs`)
4. Created `CommishSettings` model for storing Rotowire cookie (`wfbc.page/Shared/Models/CommishSettings.cs`)
5. Created `BoxScoreImport` model with `BoxScoreEntry`, `BoxScoreImportRequest`, `BoxScoreImportResult` (`wfbc.page/Shared/Models/BoxScoreImport.cs`)
6. Created server interfaces: `ISeasonTeam`, `ICommishSettings`, `IBoxScore` (`wfbc.page/Server/Interface/`)
7. Created server DALs: `SeasonTeamDataAccessLayer`, `CommishSettingsDataAccessLayer`, `BoxScoreDataAccessLayer` (`wfbc.page/Server/DataAccess/`)
8. Created `RotowireFetchService` — fetches box scores from Rotowire using cookie, sends SignalR progress updates (`wfbc.page/Server/Services/RotowireFetchService.cs`)
9. Updated `WfbcDBContext` — added `SeasonTeams`, `BoxScores` (untyped), `BoxScoresTyped` (typed), `CommishSettings`, restored `Teams`
10. Created server controllers: `SeasonTeamController`, `CommishSettingsController`, `BoxScoreController`
11. Updated `Startup.cs` — registered `ISeasonTeam`, `ICommishSettings`, `IBoxScore`, `RotowireFetchService`, `HttpClient("rotowire")` services
12. Rewrote client Teams pages — year-based routing, queries `api/seasonteam/{year}`
13. Rewrote client Managers pages — updated to show `TeamIds` dictionary
14. Created `CommishSettings.razor` — page for commish to paste/save Rotowire cookie
15. Created `UpdateBoxScores.razor` — page with year selector, progress bars, SignalR integration
16. Updated `Commish.razor` — added "Update Box Scores" and "Settings" navigation buttons
17. Fixed `AddEditDraft.razor.cs` — updated to use `manager.TeamIds[year.ToString()]`
18. Fixed `RotisserieStandingsService.cs` — replaced `_db.BoxScores[` with `_db.BoxScoresTyped[`
19. **Added `[BsonIgnoreExtraElements]` to `Manager` model** — fixes standings display bug (old MongoDB docs had `team_id` string; new model has `TeamIds` dict; driver was throwing BsonSerializationException)
20. **Updated `appsettings.json` EndYear from 2025 to 2026** — enables 2026 season support; MongoDB creates `wfbc2026` lazily on first write

## Build Status
✅ Build succeeded (warnings only, no errors)

## Runtime Status
✅ Standings display working
✅ Teams page showing existing teams from `wfbc{year}.teams` collections (data pre-existed from Python scripts)

## Architecture Notes

### Data Sources
- `wfbc.managers` — Manager documents
- `wfbc.settings` — SeasonSettings documents (one per year; contains `leagueId`, `seasonStartDate`, `seasonEndDate`)
- `wfbc.commish_settings` — single document with Rotowire cookie
- `wfbc{year}.teams` — SeasonTeam documents (already existed from Python with correct snake_case field names)
- `wfbc{year}.team_box_hitting` / `team_box_pitching` — box score data
- `wfbc{year}.standings` / `compiled_standings` — pre-computed standings

### 2026 Season
- No `wfbc2026` database exists yet; MongoDB creates it on first write
- EndYear is now 2026; `WfbcDBContext` initializes 2026 database handles
- Teams page year dropdown: `DateTime.Now.Year` (2026) down to 2019

## Next Steps (in priority order)

### 1. Fix RotowireFetchService HTTP headers
The C# service only sends `Cookie`, truncated `User-Agent`, and minimal `Accept` header. Python sends a full browser fingerprint:
- `Host: www.rotowire.com`
- `Connection: keep-alive`
- `Cache-Control: max-age=0`
- `DNT: 1`
- `Upgrade-Insecure-Requests: 1`
- `User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4147.105 Safari/537.36 Edg/84.0.522.52`
- `Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9`
- `Sec-Fetch-Site: none`, `Sec-Fetch-Mode: navigate`, `Sec-Fetch-User: ?1`, `Sec-Fetch-Dest: document`
- `Accept-Encoding: gzip, deflate, br`
- `Accept-Language: en-US,en;q=0.9,mt;q=0.8`
- `Cookie: {cookie}`

### 2. Add Season Settings Commish page (new page, separate from CommishSettings)
The `RotowireFetchService.FetchBoxScores` reads `SeasonSettings` from `wfbc.settings` collection (fields: `year`, `leagueId`, `seasonStartDate`, `seasonEndDate`). No record exists for 2026 yet. Python had these hardcoded; C# reads from DB.
- Need: new Commish page listing all season settings by year, with Add/Edit form
- Fields: year (int), season start date, season end date, league ID (Rotowire league ID)
- The `SeasonSettingsController` and `SeasonSettingsDataAccessLayer` already exist

### 3. Update UpdateBoxScores page
- Add warning if no SeasonSettings exist for the selected year
- Consider linking to the Season Settings page from the warning

### 4. After code is done (manual steps)
- Add Rotowire cookie in Settings page
- Add 2026 teams via Teams tool
- Add 2026 season settings via new Season Settings page (leagueId needed from Rotowire)
- Run box score import for 2026