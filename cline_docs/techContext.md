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
- **Primary**: MongoDB (self-hosted in Docker container, migrating from Atlas)
- **Driver**: MongoDB.Driver for .NET
- **Attributes**: MongoDB.Bson for model mapping
- **Connection**: Connection string managed via environment variables
- **Cache**: WiredTiger cache constrained to 0.25GB for 2GB droplet

### Infrastructure
- **Containerization**: Docker Compose (3 services: web, mongodb, caddy)
- **Hosting**: Digital Ocean Ubuntu Droplet (2GB RAM)
- **Domain**: wfbc.page
- **SSL**: Caddy automatic SSL (replaces Let's Encrypt/certbot)
- **Reverse Proxy**: Caddy (replaces nginx)
- **Auth Provider**: Zitadel Cloud (external, free tier)
- **Deployment**: GitHub → Docker Hub → Digital Ocean

## Development Environment

### Required Tools
- .NET 8.0 SDK
- Visual Studio or Visual Studio Code
- Docker Desktop
- Node.js (for Tailwind CSS)
- Sass compiler

### Development Setup
1. **Sass Compilation**:
   - Use `sass_workspace.code-workspace` in VSCode
   - Enable Sass Watch for automatic compilation
   - Output: `Client/wwwroot/css/styles.css`

2. **Tailwind CSS**:
   - Config: `Client/styles/tailwind/tailwind.config.js`
   - Watch command: `npx tailwindcss -i input.css -o ../../wwwroot/css/app.css --watch`
   - Production: Add `--minify` flag

3. **User Secrets** (Development):
   - Visual Studio: Right-click project → Manage User Secrets
   - CLI: `dotnet user-secrets set "DatabaseSettings:DatabaseName" "wfbc"`

### Local Development URLs
- **Application**: https://localhost:5001
- **API**: https://localhost:5010
- **Swagger**: https://localhost:5010/swagger

## Build and Deployment

### Docker Build Process
```bash
# Standard build
docker build -t tpups/wfbc-page-api:latest .

# Apple Silicon
docker buildx build --platform linux/amd64 -t tpups/wfbc-page-api:latest .
```

### Deployment Process
1. Build Docker image locally
2. Push to Docker Hub (tpups/wfbc-page-api)
3. SSH to Digital Ocean server
4. Pull latest image and restart container

### Environment Configuration
- **Development**: appsettings.json + User Secrets
- **Production**: appsettings.json + Environment Variables (.env file)

## Technical Constraints

### Authentication Requirements
- **Zitadel Cloud Configuration**:
  - PKCE-enhanced authorization code flow (User Agent/SPA type)
  - Project role-based claims mapping (claim includes project ID)
  - Custom GroupsClaimsPrincipalFactory for client-side policies
  - Pattern matching for `urn:zitadel:iam:org:project:{projectId}:roles` claims

### Database Constraints
- **MongoDB**: Self-hosted in Docker container (migrating from Atlas)
- **WiredTiger Cache**: Constrained to 0.25GB for memory-limited droplet
- **Model Requirements**: All entities must have CreatedAt/LastUpdatedAt
- **ObjectId**: String representation for cross-layer compatibility
- **Backups**: Must be handled manually (mongodump to external storage)

### Styling Constraints
- **Dual CSS Systems**: Sass for layout, Tailwind for components
- **Build Process**: Manual compilation required for Sass changes
- **Mobile Responsiveness**: AppState service tracks screen size

### Deployment Constraints
- **Docker Compose**: 3-service stack (web, mongodb, caddy)
- **Manual Build**: Docker Hub automated builds no longer available for free accounts
- **Certificate Management**: Handled automatically by Caddy
- **Memory Budget**: ~950M for containers on 2GB droplet (~1GB headroom)

## Performance Considerations
- **Blazor WebAssembly**: Client-side execution, initial load time considerations
- **MongoDB Queries**: Efficient indexing for historical data access
- **MongoDB Memory**: WiredTiger cache constrained for shared hosting
- **Image Optimization**: Docker image size management
- **CSS Bundling**: Separate files for layout vs. component styles

## Security Considerations
- **HTTPS Only**: Automatic SSL via Caddy
- **Token-based Authentication**: JWT Bearer access tokens from Zitadel Cloud
- **Role-based Authorization**: Server-side policy enforcement + client-side claim mapping
- **SSH Key Management**: Key-based server access only
- **No secrets in repo**: Zitadel authority/client ID in user secrets (dev) and .env (prod)

## Integration Points
- **Zitadel Cloud**: Authentication and authorization provider (OIDC)
- **Rotowire**: External fantasy sports data (team IDs stored in Manager model)
- **Docker Hub**: Container image registry
- **Digital Ocean**: Infrastructure hosting

## Development Workflow
1. Feature development locally with User Secrets
2. Sass/Tailwind compilation as needed
3. Local testing with development database
4. Docker build and test
5. Push to GitHub (triggers documentation/tracking)
6. Manual Docker Hub push
7. Server deployment via SSH
