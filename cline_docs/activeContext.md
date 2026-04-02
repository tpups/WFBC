# Active Context

## Current Task: Okta → Zitadel Cloud Auth Migration + Infrastructure Cleanup
**Status**: ✅ **CODE CHANGES COMPLETE** — Auth migration fully working locally, infrastructure files created

## Latest Accomplishment (April 1, 2026)

### ✅ **Okta → Zitadel Cloud Auth Migration - COMPLETE**
Successfully migrated authentication from Okta SSO to Zitadel Cloud OIDC, and created clean Docker Compose + Caddyfile for production deployment.

#### **🔑 Zitadel Cloud Setup**
- **Instance**: https://wfbc-edq5hx.us1.zitadel.cloud
- **Organization**: wfbc
- **Project**: wfbc.page (ID: 366760786435015572)
- **Application**: WFBC Web (User Agent/SPA type, PKCE auth)
- **Client ID**: 366762034123100053
- **Roles**: `Commish` and `Managers` created on project
- **Token setting**: "Return user roles during authentication" enabled

#### **🐛 Key Discovery: Zitadel Embeds Project ID in Claim Names**
- **Expected claim**: `urn:zitadel:iam:org:project:roles`
- **Actual claim**: `urn:zitadel:iam:org:project:366760786435015572:roles`
- **Solution**: Pattern matching with `StartsWith("urn:zitadel:iam:org:project:") && EndsWith(":roles")`

#### **📁 Files Modified**

**Server-side (Okta → JWT Bearer):**
- `Server/WFBC.Server.csproj` — Replaced `Okta.AspNetCore` with `Microsoft.AspNetCore.Authentication.JwtBearer`
- `Server/Startup.cs` — Replaced Okta auth with JWT Bearer + Zitadel role claim mapping via `OnTokenValidated`
- `Server/appsettings.json` — Replaced Okta config with Zitadel placeholders
- `Server/Models/AppSettings.cs` — Removed `OktaSettings`/`IOktaSettings`

**Client-side (Okta → Zitadel OIDC):**
- `Client/wwwroot/appsettings.json` — Replaced Okta config with Zitadel authority + client ID
- `Client/Program.cs` — Updated OIDC binding to `"Zitadel"` section, added `urn:zitadel:iam:org:project:roles` scope
- `Client/GroupsClaimsPrincipalFactory.cs` — Pattern-based claim matching for Zitadel's project-ID-embedded role claims

**Infrastructure (new files):**
- `docker-compose.yml` — Clean 3-service compose (web, caddy, mongodb) for Zitadel Cloud
- `Caddyfile` — Reverse proxy `wfbc.page → web:8080` with automatic SSL

**Files NOT changed (by design):**
- `Shared/Models/Policies.cs` — Claim names (`Commish`, `Managers`) still match
- All controllers — Same `[Authorize]` attributes
- All data access layers — No auth changes needed
- `Client/Pages/Authentication.razor` — Standard OIDC component unchanged

#### **💾 User Secrets Required (Development)**
```
dotnet user-secrets set "Zitadel:Authority" "https://wfbc-edq5hx.us1.zitadel.cloud"
dotnet user-secrets set "Zitadel:ClientId" "366762034123100053"
```

#### **🐳 Production .env Required**
```
DATABASE_NAME=wfbc
START_YEAR=2011
END_YEAR=2025
ZITADEL_AUTHORITY=https://wfbc-edq5hx.us1.zitadel.cloud
ZITADEL_CLIENT_ID=366762034123100053
```

## Next Steps: Production Deployment

### Remaining Tasks
1. **MongoDB Migration** — Export from Atlas (`mongodump`), import to local container (`mongorestore`)
2. **Docker build** — Build new image with auth changes and push to Docker Hub
3. **Deploy to droplet** — Upload docker-compose.yml, Caddyfile, .env; `docker compose up -d`
4. **Clean up droplet** — Remove old containers, certbot, nginx (Caddy replaces both)
5. **DNS verification** — Ensure `wfbc.page` A record points to droplet IP
6. **Create user accounts** — Add league members as Zitadel users and grant roles

### Architecture Change Summary
**Before**: App → Okta (external auth) → MongoDB Atlas (external DB)
**After**: Caddy (SSL + proxy) → App → Zitadel Cloud (external auth) → MongoDB (local container)

**Droplet resources (2GB):**
- web: 400M limit
- mongodb: 500M limit (0.25GB WiredTiger cache)
- caddy: 50M limit
- Total: ~950M → ~1GB headroom
