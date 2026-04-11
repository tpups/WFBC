# Active Context

## Current Work
Implementing box score import functionality into the .NET Blazor app, migrating functionality from Python scripts in `C:\dev\wfbc_utils`. Build succeeds. Two known runtime issues pending investigation.

## Recent Changes (numbered in order)

1. Added `LeagueId` field to `SeasonSettings` model (`wfbc.page/Shared/Models/SeasonSettings.cs`)
2. Updated `SeasonTeam` model — added `ManagerId`, `Year` fields (`wfbc.page/Shared/Models/SeasonTeam.cs`)
3. Updated `Manager` model — changed single `TeamId` (string) to `TeamIds` (Dictionary<string, string> mapping year → teamId) (`wfbc.page/Shared/Models/Manager.cs`)
4. Created `CommishSettings` model for storing Rotowire cookie (`wfbc.page/Shared/Models/CommishSettings.cs`)
5. Created `BoxScoreImport` model with `BoxScoreEntry`, `BoxScoreImportRequest`, `BoxScoreImportResult` (`wfbc.page/Shared/Models/BoxScoreImport.cs`)
6. Created server interfaces: `ISeasonTeam`, `ICommishSettings`, `IBoxScore` (`wfbc.page/Server/Interface/`)
7. Created server DALs: `SeasonTeamDataAccessLayer`, `CommishSettingsDataAccessLayer`, `BoxScoreDataAccessLayer` (`wfbc.page/Server/DataAccess/`)
8. Created `RotowireFetchService` — fetches box scores from Rotowire using cookie, sends SignalR progress updates (`wfbc.page/Server/Services/RotowireFetchService.cs`)
9. Updated `WfbcDBContext` — added:
   - `SeasonTeams`: `Dictionary<string, IMongoCollection<SeasonTeam>>` (reads `wfbc{year}.teams`)
   - `BoxScores`: `Dictionary<string, Dictionary<string, IMongoCollection<BsonDocument>>>` (new — for import DAL; reads `team_box_hitting`/`team_box_pitching` as untyped documents)
   - `BoxScoresTyped`: `Dictionary<string, Dictionary<string, IMongoCollection<Box>>>` (replaces broken original `BoxScores` — correct nested structure; used by `RotisserieStandingsService`)
   - `CommishSettings`: `IMongoCollection<CommishSettings>` (reads `wfbc.commish_settings`)
   - Restored `Teams`: `IMongoCollection<Team>` (reads `wfbc.teams` legacy flat collection)
10. Created server controllers: `SeasonTeamController`, `CommishSettingsController`, `BoxScoreController` (`wfbc.page/Server/Controllers/`)
    - `SeasonTeamController` also has a `POST api/seasonteam/migrate` endpoint that migrates legacy `wfbc.teams` records into year-specific `wfbc{year}.teams` collections (idempotent)
11. Updated `Startup.cs` — registered `ISeasonTeam`, `ICommishSettings`, `IBoxScore`, `RotowireFetchService`, `HttpClient("rotowire")` services
12. Rewrote client Teams pages — now use `SeasonTeam` with year-based routing:
    - `Teams.razor` / `Teams.razor.cs`: year selector, queries `api/seasonteam/{year}`, has "Migrate Legacy Teams" button
    - `AddEditTeam.razor` / `AddEditTeam.razor.cs`: routes `/commish/add_team/{year}` and `/commish/edit_team/{year}/{teamId}`
13. Rewrote client Managers pages — updated to show `TeamIds` dictionary (seasons): `Managers.razor`, `Managers.razor.cs`, `AddEditManager.razor`, `AddEditManager.razor.cs`
14. Created `CommishSettings.razor` — page for commish to paste/save Rotowire cookie (stored server-side)
15. Created `UpdateBoxScores.razor` — page with year selector, progress bars, SignalR integration for live fetch status
16. Updated `Commish.razor` — added "Update Box Scores" and "Settings" navigation buttons
17. Fixed `AddEditDraft.razor.cs` — updated to use `manager.TeamIds[year.ToString()]` instead of `manager.TeamId`
18. Fixed `RotisserieStandingsService.cs` — replaced all `_db.BoxScores[` with `_db.BoxScoresTyped[` to use the correctly-typed nested collection

## Build Status
✅ Build succeeded (warnings only, no errors as of last build)

## Known Issues / Pending Investigation

### Issue 1: Standings Broken ("No standings data found for 2025")
**Symptom**: The standings page shows "No standings data found for 2025. Please ensure standings have been calculated."

**Background**:
- `wfbc2025.standings` collection HAS data
- `wfbc2025.compiled_standings` collection HAS data
- `BuildUpdateStandings` process successfully ran before our session
- It stopped working during/after our session

**Likely Cause**: The original `WfbcDBContext.BoxScores` had a **pre-existing bug** — it returned `Dictionary<string, IMongoCollection<Box>>` (flat, keyed by year) but the `RotisserieStandingsService` was calling `_db.BoxScores[year]["hitting"]` which requires a **nested** dictionary. The git commit had this broken type. Our change (`BoxScores → BoxScoresTyped` with proper `Dictionary<string, Dictionary<string, IMongoCollection<Box>>>`) fixes the runtime bug. However, this doesn't explain why pre-existing standings data stopped appearing.

**Alternative Cause**: The Manager model change (`TeamId string` → `TeamIds Dictionary`) might be affecting how `RotisserieStandingsService` looks up teams when computing standings, since it may be reading manager records that no longer have the old `team_id` BSON field.

**Debugging Suggestions**:
1. Check server logs when loading the standings page — look for `[GetFinalStandingsForYearAsync]` log lines to see if the query returns 0 results
2. Check if the MongoDB filter `Builders<Standings>.Filter.Eq("Year", year)` is matching — the `Year` field in the standings documents may be stored as int, not string
3. Check if the `ServerSideStandingsCache` is serving a stale empty result from a previous failed load (cache doesn't expire)
4. Verify the `wfbc2025.standings` collection can be queried directly in MongoDB shell: `db.standings.find({Year: "2025"}).count()` — if Year is stored as int, the string filter won't match
5. The `RotisserieStandingsService` uses managers to look up teams for standings — since `Manager.TeamId` no longer exists (changed to `TeamIds`), any code in that service referencing `manager.TeamId` would silently return null/empty

**Action needed**: Search `RotisserieStandingsService.cs` for `TeamId` references that were not updated (the powershell replace only updated `BoxScores` references, not `TeamId` references).

### Issue 2: Teams Page Shows Empty (now resolved with migration button)
**Cause**: The old Teams page called `api/team` which returned ALL teams from the flat `wfbc.teams` collection. The new Teams page calls `api/seasonteam/{year}` which reads from year-specific `wfbc{year}.teams` collections that are empty until the migration runs.

**Resolution**: A "Migrate Legacy Teams" button was added to the Teams page. Clicking it calls `POST api/seasonteam/migrate` which copies all records from `wfbc.teams` into the appropriate `wfbc{year}.teams` collection based on each team's `Year` field.

## Next Steps
1. **URGENT**: Search `RotisserieStandingsService.cs` for any remaining `manager.TeamId` references and update to `manager.TeamIds`
2. Run the app and check server logs for standings query results
3. After standings fixed: run Teams migration to populate year-specific collections
4. Set Rotowire cookie in Settings, add teams, run box score import
5. Update memory bank after fixes

## Key Architecture Decisions
- `wfbc.teams` collection (old flat structure) kept for backward compatibility; new teams live in `wfbc{year}.teams`
- Box scores stored as `BsonDocument` (flexible schema) in `wfbc{year}.team_box_hitting` / `team_box_pitching`
- `BoxScoresTyped` (`IMongoCollection<Box>`) used by `RotisserieStandingsService` for standings calculations
- `BoxScores` (`IMongoCollection<BsonDocument>`) used by `BoxScoreDataAccessLayer` for importing raw data
- Rotowire cookie stored server-side in `wfbc.commish_settings` (single document, commish-only access)
- Box score fetching happens server-side (not client-side) using stored cookie