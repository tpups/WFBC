# Active Context

## Current Status: ✅ Infrastructure Migration COMPLETE (April 7, 2026)

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

## Local Development Setup
1. Docker Desktop running with MongoDB container (`docker compose up mongodb -d`)
2. Windows MongoDB service must be STOPPED and DISABLED (`sc.exe config MongoDB start=disabled`)
3. User secrets for MongoDB connection: `mongodb://localhost:27017`
4. User secrets for Zitadel auth: authority + client ID
5. App runs via `dotnet run` from Server project

## Next Steps
- Create user accounts in Zitadel for league members and grant roles
- Verify login works on production with Zitadel redirect URIs
- Set up MongoDB backup strategy (periodic mongodump to external storage)
- Consider GitHub Actions for automated Docker builds on push
