# Progress

## What Works
- League page with standings display, progression graph, wagers, funds
- Commish panel with team/manager/draft management, season settings, box score updates, standings building
- Box score fetching from Rotowire API (both full-season "Year" and daily "Today" options)
- Standings calculation with incremental daily processing and compiled documents
- Server-side and client-side caching with proper invalidation
- Timestamp-based cache validation — clients automatically detect and fetch newer standings data
- Pacific timezone handling for date calculations in box score fetching
- Value-based BSON comparison to prevent false-positive updates from type mismatches
- download_date field properly updated on box score record modifications
- **Refresh token-based silent re-auth** via `offline_access` scope (Zitadel issues refresh tokens; `AuthorizationMessageHandler` exchanges them automatically before access token expiry)
- **401 → clean re-login redirect** via `UnauthorizedRedirectHandler` DelegatingHandler when silent refresh has failed
- **24-hour role cache** on server (was 30 min) — fewer userinfo round-trips to Zitadel
- **Zitadel preconnect/dns-prefetch** hints in `index.html` for faster auth bootstrapping

## What's Left to Build
- (User manual) Configure Zitadel app to put roles directly in the JWT access token (eliminates userinfo fallback entirely)
- Optional: local-only logout path for faster sign-out (keeps Zitadel SSO cookie intact)
- Remove diagnostic logging from BoxScoreDataAccessLayer (optional cleanup)
- Potential automated box score update scheduling

## Progress Status
- Core functionality: Complete
- Box score daily update feature: Complete and tested
- Cache invalidation improvements: Complete
- Session/auth improvements: Code changes complete; production smoke test pending; manual Zitadel access-token role config pending
- Known issues: None currently blocking
