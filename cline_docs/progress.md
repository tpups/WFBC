# Progress Status

## ✅ Zitadel Auth Fix COMPLETE (April 8, 2026)

### Issues Fixed
1. **Audience mismatch** — Server validated against Client ID, but Zitadel uses Project ID in `aud`
2. **JSON array role claims** — Zitadel returns roles as array `[{...}]` not object `{...}`
3. **No roles in access token** — Zitadel doesn't include roles in JWT access tokens; added userinfo endpoint fallback with 5-minute cache
4. **Opaque tokens** — Changed Zitadel app Token Type from Opaque to JWT
5. **Client scope** — Added `urn:zitadel:iam:org:project:id:zitadel:aud` scope

### Files Modified
- `Server/Startup.cs` — Audience validation, JSON array handling, userinfo fallback with cache
- `Client/GroupsClaimsPrincipalFactory.cs` — JSON array handling for role claims
- `Client/Program.cs` — Added audience scope
- `Server/appsettings.json` — Added `Zitadel:ProjectId`
- `docker-compose.yml` — Added `ZITADEL_PROJECT_ID` env var
- `.env` — Added `ZITADEL_PROJECT_ID=366760786435015572`

### Zitadel Console Changes Required
- App Token Type: **JWT** (not Opaque)
- "Return user roles during authentication": **Enabled**

---

## ✅ Infrastructure Migration COMPLETE (April 7, 2026)

### What Was Migrated
| Component | Before | After |
|-----------|--------|-------|
| Auth | Okta SSO | Zitadel Cloud OIDC |
| Database | MongoDB Atlas (external) | MongoDB 6.0 (Docker container) |
| SSL/Proxy | nginx + certbot | Caddy (automatic SSL) |
| Container Registry | Docker Hub | GitHub Container Registry |
| Compose | Old docker-compose with Zitadel self-hosted | Clean 3-service compose (web, caddy, mongodb) |

### What's Working ✅
1. **Production site live** at https://wfbc.page
2. **Zitadel Cloud auth** — OIDC with PKCE, role-based policies
3. **MongoDB in Docker** — All 16 databases (wfbc + wfbc2011-2025) imported
4. **Caddy SSL** — Automatic certificate management
5. **GHCR** — Image at `ghcr.io/tpups/wfbc-page-api:latest`
6. **Local dev** — Docker MongoDB + user secrets working
7. **All pages** — Homepage, results, standings, drafts, commish pages
8. **Server-side caching** — Standings cache with explicit invalidation

### Files Created/Modified in Migration
**New files:**
- `docker-compose.yml` — 3-service production stack
- `Caddyfile` — Reverse proxy config
- `.env` — Production environment variables (gitignored)

**Modified files:**
- `Server/WFBC.Server.csproj` — Okta → JwtBearer package
- `Server/Startup.cs` — Okta → JWT Bearer + Zitadel claim mapping
- `Server/appsettings.json` — Okta → Zitadel config placeholders
- `Server/Models/AppSettings.cs` — Removed OktaSettings
- `Client/wwwroot/appsettings.json` — Okta → Zitadel OIDC config
- `Client/Program.cs` — Updated OIDC binding to Zitadel
- `Client/GroupsClaimsPrincipalFactory.cs` — Pattern matching for Zitadel role claims
- `.gitignore` — Added `.env` and `mongo_dump/`
- `.dockerignore` — Updated exclusions

## Remaining / Future Tasks

### Near-term
- [ ] Create Zitadel user accounts for league members and assign roles
- [ ] Verify production login with Zitadel redirect URIs
- [ ] Set up MongoDB backup strategy (periodic mongodump)
- [ ] Clean up old Docker images and orphan volumes on droplet

### Nice-to-have
- [ ] GitHub Actions for automated Docker builds on push
- [ ] MongoDB index optimization for standings queries
- [ ] Health check endpoint for monitoring
- [ ] Automated backup to external storage (DO Spaces or similar)

## Technical Notes

### Local Dev: MongoDB Port Conflict
- Windows may have a local MongoDB service on port 27017
- This shadows the Docker container's MongoDB
- Fix: `net stop MongoDB` + `sc.exe config MongoDB start=disabled` (run as admin)
- Symptom: App connects but sees empty collections

### Zitadel Role Claims
- Zitadel embeds project ID in claim names
- Expected: `urn:zitadel:iam:org:project:roles`
- Actual: `urn:zitadel:iam:org:project:366760786435015572:roles`
- Solution: Pattern matching with StartsWith/EndsWith

### Production MongoDB Security
- MongoDB port NOT exposed on host in production
- Only accessible via Docker internal network (`mongodb:27017`)
- The `ports: "27017:27017"` in local compose is for dev only
