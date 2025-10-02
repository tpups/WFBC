# System Patterns

## Architecture Overview
WFBC follows a clean, layered architecture with clear separation of concerns:

```
Client (Blazor WebAssembly) → Server (REST API) → Data Access Layer → MongoDB Atlas
```

## Key Architectural Patterns

### Repository Pattern
- **Interfaces**: Located in `wfbc.page/Server/Interface/`
  - `IDraft.cs`, `IManager.cs`, `IPick.cs`, `IStandings.cs`, `ITeam.cs`
- **Implementations**: Located in `wfbc.page/Server/DataAccess/`
  - `DraftDataAccessLayer.cs`, `ManagerDataAccessLayer.cs`, etc.
- **Controllers**: Located in `wfbc.page/Server/Controllers/`
  - `DraftController.cs`, `ManagerController.cs`, etc.

### Authentication & Authorization Pattern
- **Okta SSO Integration**: PKCE-enhanced authorization code flow
- **Claims-based Authorization**: 
  - Okta groups → .NET Claims → Policies
  - `groups: ["Everyone","Managers","Commish"]` → `Managers: Managers` and `Commish: Commish`
- **Policy-based Access Control**:
  - `[Authorize]` - Any authenticated user
  - `[Authorize(Policy = Policies.IsCommish)]` - Commissioner only
  - `[Authorize(Policy = Policies.IsManager)]` - Manager only

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
- **Shared Models**: Located in `wfbc.page/Shared/Models/`

### Client-Side Patterns
- **Dual HttpClient Pattern**:
  - `PublicClient` - Unauthenticated requests
  - `AuthenticatedClient` - Authenticated requests with access tokens
- **Component Organization**:
  - Pages: Route-specific components
  - Shared: Reusable components
  - Shared/Components: Generic UI components
  - Shared/SVG: SVG icon components

### Styling Architecture
- **Sass (Primary)**: Custom layout and navigation
  - Main styles in `Client/styles/` → `wwwroot/css/styles.css`
  - Custom CSS animations for drawer and navigation
- **Tailwind (Secondary)**: Page and component styling
  - Config: `Client/styles/tailwind/tailwind.config.js`
  - Output: `wwwroot/css/app.css`
  - Monitors: `Client/Shared` and `Client/Pages`

## Development Patterns

### Version Upgrade Pattern
- Regular .NET version upgrades (.NET Core 3.2 → 5.0 → 7.0 → 8.0)
- Dockerfile updates required with .NET version changes

### Deployment Pattern
- **Docker Containerization**: Single container for the full application
- **GitHub → Docker Hub → Digital Ocean**: Automated deployment pipeline
- **Environment Variables**: Managed via `.env` file on server
- **Image Management**: Local build → Docker Hub push → Server pull

### Configuration Pattern
- **Development**: User Secrets in Visual Studio
- **Production**: Environment variables via Docker/docker-compose
- **Settings Structure**: 
  - Client: `WFBC.Client/wwwroot/appsettings.json`
  - Server: `WFBC.Server/appsettings.json`

## Security Patterns
- **SSL/TLS**: Let's Encrypt certificates via certbot
- **SSH Access**: Key-based authentication to Digital Ocean droplet
- **Role-based UI**: Conditional rendering based on user claims
- **API Authorization**: Controller-level and action-level authorization

## Data Patterns
- **Audit Trail**: CreatedAt/LastUpdatedAt on all entities
- **External Integration**: Rotowire team ID mapping in Manager model
- **Historical Data**: Year-based organization for results and rulebooks
- **Responsive Navigation**: Conditional menus based on section and device type
