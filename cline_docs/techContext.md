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
- **Authentication**: Okta SSO with PKCE-enhanced authorization code flow
- **Authorization**: Policy-based with role claims

### Database
- **Primary**: MongoDB Atlas (Cloud-hosted)
- **Driver**: MongoDB.Driver for .NET
- **Attributes**: MongoDB.Bson for model mapping
- **Connection**: Connection string managed via environment variables

### Infrastructure
- **Containerization**: Docker
- **Hosting**: Digital Ocean Ubuntu Droplet
- **Domain**: wfbc.page
- **SSL**: Let's Encrypt certificates
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
- **Okta Configuration**:
  - PKCE-enhanced authorization code flow
  - Group-based claims mapping
  - Custom GroupsClaimsPrincipalFactory for client-side policies

### Database Constraints
- **MongoDB Atlas**: Cloud-hosted, requires internet connectivity
- **Model Requirements**: All entities must have CreatedAt/LastUpdatedAt
- **ObjectId**: String representation for cross-layer compatibility

### Styling Constraints
- **Dual CSS Systems**: Sass for layout, Tailwind for components
- **Build Process**: Manual compilation required for Sass changes
- **Mobile Responsiveness**: AppState service tracks screen size

### Deployment Constraints
- **Single Container**: Entire application in one Docker container
- **Manual Build**: Docker Hub automated builds no longer available for free accounts
- **Certificate Management**: Let's Encrypt renewal required

## Performance Considerations
- **Blazor WebAssembly**: Client-side execution, initial load time considerations
- **MongoDB Queries**: Efficient indexing for historical data access
- **Image Optimization**: Docker image size management
- **CSS Bundling**: Separate files for layout vs. component styles

## Security Considerations
- **HTTPS Only**: SSL certificate management via Let's Encrypt
- **Token-based Authentication**: Access tokens for API requests
- **Role-based Authorization**: Server-side policy enforcement
- **SSH Key Management**: Key-based server access only

## Integration Points
- **Okta SSO**: Authentication and authorization provider
- **Rotowire**: External fantasy sports data (team IDs stored in Manager model)
- **MongoDB Atlas**: Cloud database service
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
