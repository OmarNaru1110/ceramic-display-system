# Ceramic Display System - Local Development Setup

## Prerequisites
- .NET 9 SDK
- SQL Server LocalDB (comes with Visual Studio)
- Visual Studio 2022 or VS Code

## Quick Start

### 1. Clone and Setup
```bash
git clone https://github.com/OmarNaru1110/ceramic-display-system
cd ceramic-display-system/API
```

### 2. Restore Dependencies
```bash
dotnet restore
```

### 3. Run Database Migrations
```bash
cd API
dotnet ef database update
```
*Note: Migrations will be applied automatically when you run the application.*

### 4. Start the Application
```bash
cd API
dotnet run
```

## Access Points
- **API**: https://localhost:5001 or http://localhost:5000
- **Swagger/OpenAPI**: https://localhost:5001/swagger (in Development)
- **Database**: LocalDB instance (automatic)

## Database Configuration
The application uses SQL Server LocalDB by default:
- **Development DB**: `CeramicDisplaySystemDb_Dev`
- **Production DB**: `CeramicDisplaySystemDb`
- **Connection**: Windows Authentication (Trusted Connection)

## Entity Framework Commands

### Create Migration
```bash
dotnet ef migrations add [MigrationName] --project Data --startup-project API
```

### Update Database
```bash
dotnet ef database update --project Data --startup-project API
```

### Remove Last Migration
```bash
dotnet ef migrations remove --project Data --startup-project API
```

## Logging
- **Console**: Real-time logging output
- **Files**: `logs/` directory with daily rolling files
- **Development**: Debug level logging
- **Production**: Information level logging

## Development Workflow
1. Make code changes
2. The application will hot-reload automatically
3. Database migrations are applied on startup
4. Check logs in console or `logs/` folder

## Project Structure
```
??? API/           # Web API project
??? Core/          # Business logic layer  
??? Data/          # Data access layer
?   ??? Models/    # Entity models
?   ??? DataAccess/# Repositories and DbContext
?   ??? Migrations/# EF Core migrations
```

## Troubleshooting

### LocalDB Issues
```bash
# Start LocalDB manually
sqllocaldb start mssqllocaldb

# Check LocalDB status  
sqllocaldb info
```

### Port Already in Use
If ports 5000/5001 are busy, the application will automatically find available ports.

### Build Issues
```bash
# Clean and rebuild
dotnet clean
dotnet build --no-restore
```

For more help, check the application logs or contact the development team.