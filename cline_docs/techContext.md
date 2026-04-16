# Tech Context

## Technology Stack

### Frontend
- **Framework**: Blazor WebAssembly (.NET 8.0)
- **Language**: C# 
- **Styling**: 
  - Sass (primary) - Custom layout and navigation with animations
  - Tailwind CSS (secondary) - Component and page styling
- **State Management**: AppState service for application-wide state
- **UI Components**: Custom Razor components with SVG icons

### Backend
- **Framework**: ASP.NET Core (.NET 8.0)
- **API**: REST API with Swagger documentation
- **Language**: C#
- **Authentication**: Zitadel Cloud OIDC with PKCE-enhanced authorization code flow (JWT Bearer)
- **Authorization**: Policy-based with role claims from Zitadel project roles

### Database
- **Primary**: MongoDB 6.0 (self-hosted in Docker container)
- **Driver**: MongoDB.Driver for .NET
- **Attributes**: MongoDB.Bson for model mapping
- **Connection**: `mongodb://mongodb:27017` (Docker internal) / `mongodb://localhost:27017` (local dev)
- **Cache**: WiredTiger cache constrained to 0.25GB for 2GB droplet
- **Databases**: `wfbc` (main) + `wfbc2011`-`wfbc2026` (per-season data)

### Infrastructure
- **Containerization**: Docker Compose (3 services: web, mongodb, caddy)
- **Hosting**: Digital Ocean Ubuntu Droplet (2GB RAM)
- **Domain**: wfbc.page
- **SSL**: Caddy automatic SSL (replaced Let's Encrypt/certbot)
- **Reverse Proxy**: Caddy (replaced nginx)
- **Auth Provider**: Zitadel Cloud (external, free tier)
- **Container Registry**: GitHub Container Registry (ghcr.io/tpups/wfbc-page-api)
- **Source Control**: GitHub (github.com/tpups/WFBC)

## Development Environment

### Required Tools
- .NET 8.0 SDK
- Visual Studio or Visual Studio Code
- Docker Desktop (for local MongoDB)
- Node.js (for Tailwind CSS)
- Sass compiler
- MongoDB Database Tools (mongodump/mongorestore) — installed via MSI

### Development Setup
1. **Docker MongoDB**: `docker compose up mongodb -d` (from project root)
   - **IMPORTANT**: Windows MongoDB service must be STOPPED and DISABLED to avoid port 27017 conflict
   - Disable: `sc.exe config MongoDB start=disabled` (run as admin)
   
2. **User Secrets** (from `wfbc.page/Server/`):
   ```
   dotnet user-secrets set "DatabaseSettings:ConnectionString" "mongodb://localhost:27017"
   dotnet user-secrets set "DatabaseSettings:DatabaseName" "wfbc"
   dotnet user-secrets set "Zitadel:Authority" "https://wfbc-edq5hx.us1.zitadel.cloud"
   dotnet user-secrets set "Zitadel:ClientId" "366762034123100053"
   ```

3. **Sass Compilation**:
   - Use `sass_workspace.code-workspace` in VSCode
   - Enable Sass Watch for automatic compilation
   - Output: `Client/wwwroot/css/styles.css`

4. **Tailwind CSS**:
   - Config: `Client/styles/tailwind/tailwind.config.js`
   - Watch command: `npx tailwindcss -i input.css -o ../../wwwroot/css/app.css --watch`
   - Production: Add `--minify` flag

### Local Development URLs
- **Application**: https://localhost:5003 (or check launchSettings.json)
- **API**: https://localhost:5003/api/

## Build and Deployment

### Docker Build Process
```bash
# Build for GHCR
docker build -t ghcr.io/tpups/wfbc-page-api:latest .

# Push to GHCR
docker login ghcr.io -u tpups   # uses PAT with write:packages scope
docker push ghcr.io/tpups/wfbc-page-api:latest
```

### Deployment Process
1. Build Docker image locally
2. Push to GHCR (`ghcr.io/tpups/wfbc-page-api:latest`)
3. SSH to droplet as josh (`ssh josh@64.227.100.84`)
4. `cd /home/josh/wfbc && docker compose pull && docker compose up -d`
5. For file updates: `scp <files> josh@64.227.100.84:/home/josh/wfbc/`

### Droplet Access
- **SSH user**: josh
- **Root access**: Via DO console only (SSH root login disabled)
- **Project directory**: `/home/josh/wfbc/`
- **Files on droplet**: `docker-compose.yml`, `Caddyfile`, `.env`

### Environment Configuration
- **Development**: appsettings.json + User Secrets
- **Production**: appsettings.json + Environment Variables (`.env` file)

### Production .env
```
DATABASE_NAME=wfbc
START_YEAR=2011
END_YEAR=2026
ZITADEL_AUTHORITY=https://wfbc-edq5hx.us1.zitadel.cloud
ZITADEL_CLIENT_ID=366762034123100053
ZITADEL_PROJECT_ID=366760786435015572
```

## Technical Constraints

### Authentication
- Zitadel Cloud OIDC with PKCE (User Agent/SPA type)
- Token Type: **JWT** (must be set in Zitadel app settings; default is opaque)
- Audience: Project ID (`366760786435015572`) — Zitadel puts this in `aud`, not client ID
- Project role-based claims mapping (claim includes project ID)
- Pattern matching: `urn:zitadel:iam:org:project:{projectId}:roles`
- Roles come as **JSON array** format, not object (both client and server handle this)
- Custom `GroupsClaimsPrincipalFactory` for client-side policies (parses ID token roles)
- Server-side: `OnTokenValidated` handler with **userinfo endpoint fallback** + 5-minute cache
  (Zitadel doesn't include roles in JWT access tokens, only in ID tokens and userinfo)
- Client scopes: `openid`, `profile`, `email`, `urn:zitadel:iam:org:project:roles`, `urn:zitadel:iam:org:project:id:zitadel:aud`

### Database
- MongoDB 6.0 in Docker container
- WiredTiger cache: 0.25GB (memory-limited droplet)
- All entities: CreatedAt/LastUpdatedAt required
- ObjectId: String representation for cross-layer compatibility
- Backups: Manual `mongodump` to external storage
- Production: MongoDB NOT exposed on host port (Docker internal only)

### Memory Budget (2GB Droplet)
- web: 400M limit
- mongodb: 500M limit
- caddy: 50M limit
- Total: ~950M → ~1GB headroom for OS

### Styling
- Dual CSS: Sass for layout, Tailwind for components
- Manual compilation required for Sass changes
- Mobile responsiveness via AppState screen size tracking

## Security
- **HTTPS**: Automatic SSL via Caddy
- **Auth**: JWT Bearer tokens from Zitadel Cloud
- **Authorization**: Server-side policy enforcement + client-side claim mapping
- **SSH**: Key-based (josh user), root via DO console only
- **No secrets in repo**: Auth config in user secrets (dev) and .env (prod)
- **MongoDB**: Not exposed to internet in production (Docker internal network only)

### Rotowire HTTP Client
- Named client "rotowire" configured with `AutomaticDecompression` (gzip/deflate/brotli) via `ConfigurePrimaryHttpMessageHandler`
- Do NOT add manual `Accept-Encoding` header — the handler manages it automatically
- Full browser fingerprint headers required (User-Agent, Sec-Fetch-*, etc.)
- Cookie obtained from browser DevTools → Network tab → Request Headers → Cookie

### SignalR Authentication
- WebSocket protocol can't send Authorization headers
- Server extracts token from `?access_token=` query parameter for `/progressHub` (configured in `OnMessageReceived` JwtBearer event)
- Client configures `AccessTokenProvider` on `HubConnectionBuilder`

### Box Score Import — Int64 vs Int32
- Python scripts stored numeric values as Int32 in MongoDB
- .NET `System.Text.Json` deserializes numbers via `TryGetInt64` → stores as Int64
- `ConvertToInt()` in `RotisserieStandingsService.cs` handles `int`, `long`, `double`, `decimal`, `short`, `string`
- IP (innings pitched) stored as string, handled separately

## Integration Points
- **Zitadel Cloud**: Authentication and authorization (OIDC)
- **Rotowire**: Box score data fetched via HTTP with browser fingerprint + cookie auth
- **GitHub Container Registry**: Docker image hosting
- **Digital Ocean**: Infrastructure hosting
