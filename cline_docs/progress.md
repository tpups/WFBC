# Progress Status

## ✅ Styling Fixes COMPLETE (April 17, 2026)

Fixed styling regressions from Advanced tab addition: standings table bottom blue bar (changed fixed height to max-height), chart horizontal scrollbar (replaced `100vw` with `document.documentElement.clientWidth` + scrollbar buffer), and advanced cards mobile overflow.

### Files Modified
- `Client/Shared/Components/StandingsTable.razor` — `h-[60dvh]` → `max-h-[60dvh]`
- `Client/Shared/Components/StandingsGraph.razor` — JS viewport calc + CSS media query updates
- `Client/Shared/MainLayout.razor` — verified `overflow-auto` on main element

---

## ✅ Advanced Stats Tab COMPLETE (April 16, 2026)

Added "Advanced" tab to results pages with leaderboard cards for non-scoring advanced statistics.

### What Was Done
- Created `AdvancedStats` model — separate bucket for Starts (GS), Quality Start Rate (QS/GS), Strikeout Rate (K/PA), Walk Rate (BB/PA)
- Extended `RotisserieStandingsService` — aggregates GS (pitching) and K (batting), computes rates in `PopulateAdvancedStats`
- Created `AdvancedStandings.razor` — declarative card-grid component with ranked leaderboards per category
- Added third tab to `StandingsDisplay.razor` — reuses existing final standings data, no new API calls
- Fixed missing `K` mapping in `GetHittingStatValue` that caused 0% strikeout rate
- Added league averages to each advanced stat card header (right-aligned "Lg Avg" with computed value)

### Files Created
- `Shared/Models/AdvancedStats.cs`
- `Client/Shared/Components/AdvancedStandings.razor`

### Files Modified
- `Shared/Models/Standings.cs` — Added `Advanced` property (`[BsonIgnoreIfNull]`)
- `Server/Services/RotisserieStandingsService.cs` — GS/K aggregation, `PopulateAdvancedStats`, `GetHittingStatValue` K mapping
- `Client/Shared/Components/StandingsDisplay.razor` — Third tab button + content region

---

## ✅ Box Score Import & 2026 Season COMPLETE (April 15, 2026)

Full migration of Python box score import functionality into .NET Blazor app. 2026 season fully operational.

### What Was Done
- Migrated Rotowire box score fetching from Python scripts to `RotowireFetchService.cs`
- Created Commish panel tools: Update Box Scores, Commish Settings (cookie), Season Settings Manager
- Fixed gzip decompression: `HttpClientHandler` with `AutomaticDecompression` + removed manual `Accept-Encoding`
- Fixed SignalR auth: server reads `access_token` from query string for WebSocket `/progressHub`
- Fixed standings `ConvertToInt()`: handles `long` (Int64) values from .NET JSON importer (Python stored Int32)
- Updated nav for 2026: `NavMenu.razor` loop starts at 2026, `MainLayout.razor` default link → `/results/2026`
- `ResultsDynamic.razor` already had 2026 league ID (16) mapped

### Files Created
- `Server/Services/RotowireFetchService.cs` — Rotowire fetch with SignalR progress
- `Server/DataAccess/BoxScoreDataAccessLayer.cs` — MongoDB import with BsonDocument conversion
- `Server/DataAccess/SeasonTeamDataAccessLayer.cs`
- `Server/DataAccess/CommishSettingsDataAccessLayer.cs`
- `Server/Controllers/BoxScoreController.cs`, `SeasonTeamController.cs`, `CommishSettingsController.cs`
- `Server/Interface/IBoxScore.cs`, `ISeasonTeam.cs`, `ICommishSettings.cs`
- `Shared/Models/BoxScoreImport.cs`, `CommishSettings.cs`
- `Client/Pages/Commish/UpdateBoxScores.razor`, `CommishSettings.razor`, `SeasonSettingsManager.razor`

### Files Modified
- `Server/Startup.cs` — HttpClient with AutomaticDecompression, SignalR OnMessageReceived token extraction
- `Server/Services/RotisserieStandingsService.cs` — `ConvertToInt()` handles long/double/decimal/short
- `Server/Models/WfbcDBContext.cs` — Added BoxScores, BoxScoresTyped, SeasonTeams, CommishSettings
- `Client/Shared/NavMenu.razor` — Results loop starts at 2026
- `Client/Shared/MainLayout.razor` — Default Results link → /results/2026
- `Client/Pages/Commish/Commish.razor` — New nav buttons
- `Shared/Models/SeasonSettings.cs` — Added LeagueId
- `Shared/Models/SeasonTeam.cs` — Added ManagerId, Year
- `Shared/Models/Manager.cs` — TeamIds dict + BsonIgnoreExtraElements

---

## ✅ Rulebook Accordion Conversion COMPLETE (April 8, 2026)

Converted all 10 rulebook pages to collapsible accordion sections using HTML5 `<details>`/`<summary>`.

### What Was Done
- All 10 rulebook pages (2011-2026) converted from flat text to accordion format
- Each rulebook section is a collapsible `<details>` element, all starting collapsed
- "Expand All / Collapse All" button via JS interop (`site.js`)
- Custom SCSS (`_accordion.scss`) for marker hiding, arrow rotation, width constraints
- Fixed width inconsistency: added `w-full` to outer container (flex-row parent was sizing to content)

### Files Created
- `wwwroot/js/site.js` — JS interop for accordion toggle
- `styles/_accordion.scss` — Custom accordion styles

### Files Modified
- All 10 `Rulebook*.razor` files — Accordion conversion + `w-full` on outer div
- `styles/styles.scss` — Added `@import 'accordion'`
- `index.html` — Added `site.js` reference, cache-busting (`styles.css?ver2.2`, `site.js?ver1.0`)

---

## ✅ Zitadel Auth Fix COMPLETE (April 8, 2026)

### Issues Fixed
1. **Audience mismatch** — Server validated against Client ID, but Zitadel uses Project ID in `aud`
2. **JSON array role claims** — Zitadel returns roles as array `[{...}]` not object `{...}`
3. **No roles in access token** — Zitadel doesn't include roles in JWT access tokens; added userinfo endpoint fallback with 5-minute cache
4. **Opaque tokens** — Changed Zitadel app Token Type from Opaque to JWT
5. **Client scope** — Added `urn:zitadel:iam:org:project:id:zitadel:aud` scope

---

## ✅ Infrastructure Migration COMPLETE (April 7, 2026)

### What Was Migrated
| Component | Before | After |
|-----------|--------|-------|
| Auth | Okta SSO | Zitadel Cloud OIDC |
| Database | MongoDB Atlas (external) | MongoDB 7.0 (Docker container) |
| SSL/Proxy | nginx + certbot | Caddy (automatic SSL) |
| Container Registry | Docker Hub | GitHub Container Registry |
| Compose | Old docker-compose with Zitadel self-hosted | Clean 3-service compose (web, caddy, mongodb) |

### What's Working ✅
1. **Production site live** at https://wfbc.page
2. **Zitadel Cloud auth** — OIDC with PKCE, role-based policies
3. **MongoDB in Docker** — All databases (wfbc + wfbc2011-2026) 
4. **Caddy SSL** — Automatic certificate management
5. **GHCR** — Image at `ghcr.io/tpups/wfbc-page-api:latest`
6. **Local dev** — Docker MongoDB + user secrets working
7. **All pages** — Homepage, results, standings, drafts, commish pages
8. **Rulebook accordions** — All 10 rulebook pages with collapsible sections
9. **Server-side caching** — Standings cache with explicit invalidation
10. **Box score import** — Full Rotowire integration via Commish panel

## Remaining / Future Tasks

### Near-term
- [ ] Deploy latest changes to production
- [ ] Create Zitadel user accounts for league members and assign roles
- [ ] Set up MongoDB backup strategy (periodic mongodump)
- [ ] Add 2026 Trophy entry at end of season

### Nice-to-have
- [ ] GitHub Actions for automated Docker builds on push
- [ ] Automated/scheduled box score imports
- [ ] MongoDB index optimization for standings queries
- [ ] Health check endpoint for monitoring
- [ ] Automated backup to external storage (DO Spaces or similar)

## Technical Notes

### Box Score Import — Int64 vs Int32
- Python scripts stored numeric values as Int32 in MongoDB
- .NET `System.Text.Json` deserializes numbers via `TryGetInt64` → stores as Int64
- `ConvertToInt()` in `RotisserieStandingsService.cs` must handle both types
- IP (innings pitched) stored as string, handled separately

### Rotowire HTTP Client
- Named client "rotowire" configured with `AutomaticDecompression` (gzip/deflate/brotli)
- Do NOT add manual `Accept-Encoding` header — the handler manages it
- Full browser fingerprint headers required (User-Agent, Sec-Fetch-*, etc.)
- Cookie obtained from browser DevTools → Network tab → Request Headers → Cookie

### SignalR Authentication
- WebSocket protocol can't send Authorization headers
- Server extracts token from `?access_token=` query parameter for `/progressHub`
- Client configures `AccessTokenProvider` on `HubConnectionBuilder`

### Local Dev: MongoDB
- Run `docker compose up -d mongodb` to start local MongoDB
- Docker Desktop must be running first
- Data persists in `mongo_data` Docker volume

### Zitadel Role Claims
- Zitadel embeds project ID in claim names
- Expected: `urn:zitadel:iam:org:project:roles`
- Actual: `urn:zitadel:iam:org:project:366760786435015572:roles`
- Solution: Pattern matching with StartsWith/EndsWith