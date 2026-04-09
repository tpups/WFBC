# Active Context

## Current Status: ✅ Infrastructure Migration + Auth Fix COMPLETE (April 8, 2026)

All services migrated and production site is live at https://wfbc.page

## What Was Done

### 1. Auth Migration: Okta → Zitadel Cloud
- Replaced Okta SSO with Zitadel Cloud OIDC (PKCE-enhanced)
- Server: JWT Bearer auth with Zitadel role claim mapping
- Client: OIDC binding to Zitadel, pattern-based claim matching for project-ID-embedded roles
- Key discovery: Zitadel embeds project ID in claim names, requiring pattern matching

### 2. Database Migration: MongoDB Atlas → Self-hosted Docker
- Exported all databases from Atlas via `mongodump`
- Imported to Docker-hosted MongoDB via `mongorestore`
- WiredTiger cache constrained to 0.25GB for 2GB droplet
- Local dev note: Must disable Windows MongoDB service to avoid port 27017 conflict with Docker

### 3. Container Registry: Docker Hub → GitHub Container Registry (GHCR)
- Image now at `ghcr.io/tpups/wfbc-page-api:latest`
- Free private repos, integrated with GitHub account
- Login: `docker login ghcr.io -u tpups` (uses PAT with `write:packages` scope)

### 4. SSL/Proxy: nginx + certbot → Caddy
- Caddy handles automatic SSL and reverse proxy
- Simple Caddyfile: `wfbc.page { reverse_proxy web:8080 }`

### 5. Docker Compose Rewrite
- 3-service stack: web, caddy, mongodb
- Environment variables from `.env` file
- Memory-budgeted: web 400M, mongodb 500M, caddy 50M (~950M total on 2GB droplet)

### 6. Zitadel Auth Fix (April 7-8, 2026)
Fixed 401 errors on Commish API calls (season settings, standings). Three issues found and resolved:

**Issue A — Audience Mismatch**: Server validated JWT audience against Client ID, but Zitadel puts Project ID in the `aud` claim.
- Fix: Added `Zitadel:ProjectId` config and updated `Startup.cs` to accept both Project ID and Client ID as valid audiences.
- Files: `Startup.cs`, `appsettings.json`, `docker-compose.yml`, `.env`, user secrets

**Issue B — JSON Array Role Claims**: Zitadel returns the roles claim as a JSON array `[{...}]`, not object `{...}`. Both `GroupsClaimsPrincipalFactory` and `Startup.cs OnTokenValidated` called `EnumerateObject()` which fails on arrays.
- Fix: Updated both parsers to detect `JsonValueKind.Array` vs `JsonValueKind.Object` and handle both, with deduplication via `HashSet`.
- Files: `GroupsClaimsPrincipalFactory.cs`, `Startup.cs`

**Issue C — No Role Claims in Access Token**: Zitadel includes role claims in ID tokens but NOT in JWT access tokens. The server receives the access token, validates it, but finds no role claims → Commish policy fails.
- Fix: Server-side `OnTokenValidated` falls back to Zitadel's `/oidc/v1/userinfo` endpoint when no role claims are in the access token. Results are cached in `IMemoryCache` for 5 minutes per user to avoid repeated calls.
- Files: `Startup.cs`

**Issue D (Prerequisite)**: Zitadel issues opaque access tokens by default for SPA apps.
- Fix: Changed Token Type from "Opaque" to "JWT" in Zitadel console app settings.

**Additional client change**: Added `urn:zitadel:iam:org:project:id:zitadel:aud` scope to request project audience in access tokens.
- Files: `Client/Program.cs`

### 7. Rulebook Accordion Conversion (April 8, 2026)
Converted all 10 rulebook pages from flat text to collapsible accordion sections.

**Changes across all 10 rulebook pages** (Rulebook11, 14, 16, 20, 21, 22, 23, 24, 25, 26):
- Each section wrapped in `<details class="rulebook-section">` with `<summary>` header
- All sections start collapsed
- "Expand All / Collapse All" toggle button with JS interop via `site.js`
- Added `w-full` to outer container div to fix width inconsistency in flex-row layout
- Custom SCSS (`_accordion.scss`) for marker hiding, arrow rotation, and width constraints
- `site.js` added to `wwwroot/js/` with `rulebookAccordion.toggleAll` function
- Cache-busting added to `index.html`: `styles.css?ver2.2`, `site.js?ver1.0`

**Key files created/modified**:
- `wwwroot/js/site.js` (new) — JS interop for expand/collapse all
- `styles/_accordion.scss` (new) — custom details/summary styles
- `styles/styles.scss` — added `@import 'accordion'`
- `index.html` — added site.js reference + cache busting
- All 10 `Rulebook*.razor` files — converted to accordion format

### 8. Mobile Viewport Fix (April 8, 2026)
Fixed content being cut off at the bottom on iPhone 17 Pro (and other mobile devices with browser toolbars/notches).

**Root Cause**: `.drawer-container` used `height: 100vh`, which on mobile browsers includes the area behind the browser's bottom toolbar. This made the `.main` scrollable area extend behind the toolbar, cutting off the last ~50-80px of content (trophy bottom row, last-place teams in results, etc.). Chrome DevTools can't reproduce this because it doesn't simulate dynamic browser chrome.

**Fixes**:
- `_drawer.scss`: Changed `height: 100vh` → `height: 100dvh` (dynamic viewport height that accounts for mobile browser UI)
- `index.html`: Added `viewport-fit=cover` to viewport meta tag for proper safe-area handling on notched devices
- `styles.scss`: Added `padding-bottom: env(safe-area-inset-bottom)` to `.main` for home indicator area on iPhones

**Note**: Requires Sass recompilation via VS Code Sass Watch extension before deployment.

## Production Deployment Details
- **Droplet**: Digital Ocean Ubuntu (`docker-ubuntu-s-1vcpu-1gb-sfo3-01`)
- **SSH user**: josh (`/home/josh/wfbc/`)
- **Root access**: Via DO console only (SSH root login disabled)
- **Files on droplet**: `docker-compose.yml`, `Caddyfile`, `.env` (no MongoDB port exposed)

## Zitadel Cloud Details
- **Instance**: https://wfbc-edq5hx.us1.zitadel.cloud
- **Project**: wfbc.page (ID: 366760786435015572)
- **Client ID**: 366762034123100053
- **Roles**: `Commish` and `Managers`
- **Redirect URIs**: Both `localhost` (dev) and `wfbc.page` (prod) configured
- **Token Type**: JWT (changed from default Opaque)
- **"Return user roles during authentication"**: Enabled at project level

## Local Development Setup
1. Docker Desktop running with MongoDB container (`docker compose up mongodb -d`)
2. Windows MongoDB service must be STOPPED and DISABLED (`sc.exe config MongoDB start=disabled`)
3. User secrets for MongoDB connection: `mongodb://localhost:27017`
4. User secrets for Zitadel auth: authority + client ID + project ID
5. App runs via `dotnet run` from Server project

## Next Steps
- Recompile Sass and rebuild Docker image with accordion + viewport fixes
- Deploy to production
- Create user accounts in Zitadel for league members and grant roles
- Set up MongoDB backup strategy (periodic mongodump to external storage)
- Consider GitHub Actions for automated Docker builds on push
