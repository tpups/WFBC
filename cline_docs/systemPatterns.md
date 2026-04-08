# System Patterns

## Architecture Overview
WFBC follows a clean, layered architecture with clear separation of concerns:

```
Caddy (SSL + Reverse Proxy) → .NET Server (REST API) → MongoDB (Docker)
                              ↕
                    Blazor WebAssembly Client
                              ↕
                    Zitadel Cloud (OIDC Auth)
```

## Infrastructure Pattern

### Docker Compose Stack (3 services)
- **web**: .NET 8.0 app (Blazor WASM hosted) — 400M memory limit
- **caddy**: Automatic SSL + reverse proxy — 50M memory limit
- **mongodb**: MongoDB 6.0 with constrained cache — 500M memory limit

### Environment Configuration
- **Development**: `appsettings.json` + User Secrets (dotnet user-secrets)
- **Production**: `appsettings.json` + `.env` file → Docker Compose environment variables
- **Pattern**: `appsettings.json` has placeholder values, overridden by secrets/env vars

### Deployment Pipeline
1. Build Docker image locally
2. Push to GitHub Container Registry (`ghcr.io/tpups/wfbc-page-api`)
3. SSH to droplet, pull image, restart containers
4. Caddy handles SSL certificate provisioning automatically

## Key Architectural Patterns

### Repository Pattern
- **Interfaces**: `wfbc.page/Server/Interface/` (IDraft, IManager, IPick, IStandings, ITeam)
- **Implementations**: `wfbc.page/Server/DataAccess/` (XxxDataAccessLayer.cs)
- **Controllers**: `wfbc.page/Server/Controllers/` (REST endpoints)

### Authentication & Authorization Pattern
- **Zitadel Cloud OIDC**: PKCE-enhanced authorization code flow (SPA type)
- **JWT Bearer**: Server validates JWT access tokens from Zitadel
- **Token Type**: Must be set to "JWT" in Zitadel app settings (default is opaque)
- **Audience Validation**: Accepts both Project ID and Client ID (Zitadel puts project ID in `aud`)
- **Claims-based Authorization**: 
  - Zitadel project roles → claims → .NET Policies
  - Claim name includes project ID: `urn:zitadel:iam:org:project:{projectId}:roles`
  - **Client-side**: `GroupsClaimsPrincipalFactory` parses role claims from ID token
  - **Server-side**: `OnTokenValidated` handler in `Startup.cs` maps roles to flat claims
  - Handles both JSON array and object formats (Zitadel returns arrays)
  - Deduplication via `HashSet<string>` since arrays may contain duplicates
- **Userinfo Fallback**: Zitadel doesn't include roles in JWT access tokens by default.
  Server falls back to `/oidc/v1/userinfo` endpoint when no role claims in access token.
  Results cached in `IMemoryCache` for 5 minutes per user (keyed by subject ID).
- **Policy-based Access Control**:
  - `[Authorize]` — Any authenticated user
  - `[Authorize(Policy = Policies.IsCommish)]` — Commissioner only
  - `[Authorize(Policy = Policies.IsManager)]` — Manager only

### Data Model Pattern
- **MongoDB Integration**: Using MongoDB.Bson attributes
- **Consistent Model Structure**:
  ```csharp
  [BsonId]
  [BsonRepresentation(BsonType.ObjectId)]
  public string? Id { get; set; }
  
  [Required]
  public DateTime? CreatedAt { get; set; }
  
  [Required]
  public DateTime? LastUpdatedAt { get; set; }
  ```
- **Shared Models**: `wfbc.page/Shared/Models/` (used by both client and server)

### Multi-Database Pattern
- **Main database** (`wfbc`): Managers, teams, drafts, picks, season settings
- **Per-season databases** (`wfbc2011`-`wfbc2025`): Standings, compiled standings, box scores
- **WfbcDBContext**: Manages connections to multiple databases via `Dictionary<string, IMongoCollection>`

### Caching Pattern
- **ServerSideStandingsCache**: In-memory cache for standings data
  - Indefinite cache with explicit invalidation on recalculation
  - Reduces MongoDB queries by 90%+
  - Three cache keys per year: final standings, progression data, last updated timestamp
- **Client StandingsCacheService**: Browser-side caching with `Last-Modified` headers

### Client-Side Patterns
- **Dual HttpClient Pattern**:
  - `PublicClient` — Unauthenticated requests
  - `AuthorizedClient` — Authenticated requests with access tokens
- **Component Organization**:
  - Pages: Route-specific components
  - Shared: Reusable components
  - Shared/Components: Generic UI components (StandingsTable, Modal, Tooltip)
  - Shared/SVG: SVG icon components

### Styling Architecture
- **Sass (Primary)**: Custom layout and navigation
  - Main styles in `Client/styles/` → `wwwroot/css/styles.css`
  - Custom CSS animations for drawer and navigation
- **Tailwind (Secondary)**: Page and component styling
  - Config: `Client/styles/tailwind/tailwind.config.js`
  - Output: `wwwroot/css/app.css`

## Security Patterns
- **SSL/TLS**: Automatic via Caddy (replaces Let's Encrypt/certbot)
- **SSH Access**: Key-based authentication (josh user), root via DO console only
- **MongoDB**: Not exposed to internet in production (Docker internal network only)
- **No secrets in repo**: `.env` and user secrets excluded from git
- **Role-based UI**: Conditional rendering based on user claims
- **API Authorization**: Controller-level and action-level authorization

## Data Patterns
- **Audit Trail**: CreatedAt/LastUpdatedAt on all entities
- **External Integration**: Rotowire team ID mapping in Manager model
- **Historical Data**: Year-based database organization for results (2011-2025)
- **Responsive Navigation**: Conditional menus based on section and device type
