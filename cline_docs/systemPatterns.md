# System Patterns

## Architecture
- **Blazor WebAssembly** hosted on ASP.NET Core server
- **MongoDB** database (Docker container, Mongo 7.0)
- **Caddy** reverse proxy with automatic SSL
- **Docker Compose** for production deployment (3 services: web, caddy, mongodb)

## Server-Side Patterns

### Database Context (`WfbcDBContext`)
- Single `wfbc` database for cross-season data (managers, settings, commish_settings)
- Year-specific databases (`wfbc2011` through `wfbc2026`) for season data
- `BoxScores[year][type]` — untyped `BsonDocument` collections for import
- `BoxScoresTyped[year][type]` — typed `Box` model collections for querying
- `SeasonTeams[year]` — teams per season
- `CompiledFinalStandings[year]` / `CompiledProgressionData[year]` — optimized standings

### Data Access Layer Pattern
- Interface + Implementation: `IBoxScore` → `BoxScoreDataAccessLayer`
- Registered as `AddTransient<>` in `Startup.cs`
- Direct MongoDB driver usage (no ORM)

### Box Score Import Pipeline
1. Client sends year + SignalR connectionId to `BoxScoreController`
2. Controller reads cookie from `CommishSettings` collection
3. `RotowireFetchService` fetches from Rotowire with browser fingerprint headers
4. JSON deserialized to `Dictionary<string, object?>` (values are `JsonElement`)
5. `BoxScoreDataAccessLayer.ConvertToBson()` converts `JsonElement` → native types (long for numbers, string for strings)
6. Upsert logic: find by teamID + stats_date + player + position; insert new or update changed stats
7. SignalR progress sent back to client via `ReceiveProgress`

### Standings Calculation
- `RotisserieStandingsService` — loads box scores, aggregates stats, calculates rotisserie points
- Incremental daily processing with pre-loaded season data for performance
- Compiled documents (2 per year) replace thousands of individual standings docs
- `ConvertToInt()` handles multiple numeric types: `int`, `long`, `double`, `decimal`, `short`, `string`
- IP (innings pitched) handled as string with special decimal parsing

### HTTP Client Configuration
- Named client "rotowire" with `AutomaticDecompression` (gzip/deflate/brotli)
- Full browser fingerprint headers (User-Agent, Sec-Fetch-*, Cookie, etc.)
- Do NOT manually set `Accept-Encoding` — handler manages it

### Authentication
- Zitadel Cloud OIDC with JWT Bearer
- Role claims extracted from userinfo endpoint (cached 30 min via IMemoryCache)
- Userinfo HTTP calls use named `IHttpClientFactory` client ("zitadel") for connection reuse
- SignalR uses query string `access_token` parameter (WebSocket limitation)
- SignalR pages use `EnsureHubConnected()` pattern to detect token expiry and rebuild connections
- Both `UpdateBoxScores` and `BuildUpdateStandings` use `.WithAutomaticReconnect()`
- 401 responses show "Your session has expired" instead of generic errors
- Logout includes `post_logout_redirect_uri` for immediate redirect back to app
- Policies: `IsCommish`, `IsManager`

### Caching
- `ServerSideStandingsCache` — in-memory cache with explicit invalidation after standings rebuild
- `IMemoryCache` — 30-minute cache for Zitadel userinfo role lookups

## Client-Side Patterns

### Navigation
- Sidebar drawer with collapsible sections (Results, Rulebook)
- `NavMenu.razor` generates year links dynamically (2026 down to 2011)
- `ResultsDynamic.razor` handles years 2019+ with parameterized route `/results/{year:int}`
- Static result pages for 2011-2018

### Commish Panel
- Protected by `IsCommish` policy
- Tools: Teams, Managers, Drafts, Standings, Update Box Scores, Season Settings, Settings
- SignalR for real-time progress on long operations

### State Management
- `AppState` service for UI state (drawer open/closed, mobile detection)
- Cascading parameters for nav section context