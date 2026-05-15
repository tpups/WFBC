# Active Context

## What We're Working On Now
Completed: Session/auth improvements — refresh tokens, 401 redirect handler, longer role cache, Zitadel preconnect hints.

## Recent Changes
1. Added `offline_access` scope in `Client/Program.cs` so Zitadel issues refresh tokens (requires "Refresh Token" enabled on the Zitadel app — already toggled on)
2. Created `Client/Services/UnauthorizedRedirectHandler.cs` — `DelegatingHandler` that catches 401s from API calls and redirects to `authentication/login` with the current URL as the return target (clean re-auth when silent refresh has truly failed)
3. Registered `UnauthorizedRedirectHandler` on the `AuthorizedClient` HttpClient pipeline in `Program.cs` (chained after `AuthorizationMessageHandler`)
4. Extended Zitadel role cache TTL in `Server/Startup.cs` from 30 minutes to 24 hours (roles rarely change; logout/login refreshes if needed)
5. Added `<link rel="preconnect">` and `<link rel="dns-prefetch">` for the Zitadel authority in `Client/wwwroot/index.html` to warm the TLS handshake before silent renewal iframe fires
6. (Side note: had to recreate `index.html` after a failed `replace_in_file` lost the file — recovered with original content + new hints)

## Pending User Action
- **Manual Zitadel config**: Move roles into the JWT access token via Zitadel Console → Project → Applications → SPA → Token Settings. Enable "User roles inside ID Token", "User Info inside ID Token", and the equivalent toggle for adding roles to the access token. Once roles ride in the access token, the server's userinfo fallback in `OnTokenValidated` never fires.
- **Production smoke test**: Deploy these changes, leave a tab open overnight, try a commish action the next day. With refresh tokens enabled, the access token should silently renew and actions should just work.

## Session Duration (with refresh tokens enabled)
- Access token lifetime: 12h (Zitadel default)
- Refresh token idle expiration: 30 days (must use at least once per 30d)
- Refresh token absolute expiration: 90 days (then full re-auth required)
- Net effect: commish stays logged in for up to 90 days continuously as long as they visit at least every 30 days

## Next Steps
- Apply the manual Zitadel access-token roles config
- Monitor for half-day-stale recurrence (should be fixed)
- Optional Phase 3: implement a "local-only logout" path (clear WASM auth state + reload, leave Zitadel SSO cookie intact) for faster sign-out — held until Phase 1/2 are validated in production
- Remove diagnostic logging from BoxScoreDataAccessLayer once confirmed stable (carried over from prior work)