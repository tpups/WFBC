# Product Context

## Project Purpose
WFBC (World Fantasy Baseball Classic) is a fantasy baseball web application that serves as a comprehensive platform for record keeping from multiple seasons of a fantasy baseball league. The project began development in October 2020 and has evolved through multiple .NET versions (.NET Core 3.2 → .NET 5.0 → .NET 7.0 → .NET 8.0).

## Problems It Solves
- **League Management**: Centralized management of fantasy baseball league operations including managers, teams, drafts, and standings
- **Historical Record Keeping**: Maintains detailed historical records dating back to 2011 with rulebooks and season results
- **Access Control**: Role-based authorization system distinguishing between commissioners ("Commish") and regular managers
- **Draft Management**: Handles draft picks and draft organization across multiple seasons
- **External Integration**: Integrates with Rotowire for team data and statistics
- **Multi-Season Support**: Tracks league activity across multiple years with comprehensive historical data
- **Mobile Access**: Responsive design enables league management from any device

## How It Should Work
- **Authentication**: Users authenticate through Zitadel Cloud OIDC using PKCE-enhanced authorization code flow with role-based access based on Zitadel project roles
- **Commissioner Functions**: Commissioners can manage all aspects of the league including adding/editing managers, teams, drafts, and standings
- **Manager Access**: Regular managers have limited access to view league information and manage their own data
- **Historical Access**: Users can browse historical rulebooks (2011-2025) and season results (2011-2025)
- **Responsive Design**: Works across desktop, tablet, and mobile devices with adaptive navigation
- **Data Management**: Persistent storage of all league data including historical records in self-hosted MongoDB
- **API Access**: REST API for development and integration

## Target Users
- **League Commissioners**: Full administrative access to manage all league operations
- **League Managers**: Limited access to participate in league activities and view information
- **Historical Browsers**: Anyone with access can view historical league data and rulebooks

## Key Features
- Multi-year league management
- Draft management and tracking
- Manager and team administration
- Standings calculation and display with progression graphs
- Historical record preservation
- Role-based access control (Zitadel Cloud)
- Mobile-responsive design
- Comprehensive audit trails with CreatedAt/LastUpdatedAt timestamps
- Server-side standings caching for performance

## Infrastructure
- **Hosting**: Digital Ocean 2GB droplet running Docker Compose
- **Database**: Self-hosted MongoDB 6.0 (migrated from MongoDB Atlas)
- **Auth**: Zitadel Cloud free tier (migrated from Okta)
- **SSL**: Automatic via Caddy (replaced nginx + certbot)
- **Container Registry**: GitHub Container Registry (replaced Docker Hub)
- **Domain**: wfbc.page
