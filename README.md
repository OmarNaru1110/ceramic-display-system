# Ceramic Display System

Ceramic Display System is a sample ASP.NET Core Web API used to manage products, orders, invoices, users and media for a fictional ceramic storefront. The repository includes an API project, core libraries, data access layers and a docker-compose configuration to run the full stack locally (SQL Server, MinIO, Adminer and the Web API).

## Table of Contents
- [Ceramic Display System](#ceramic-display-system)
  - [Table of Contents](#table-of-contents)
  - [Features](#features)
  - [Architecture](#architecture)
  - [Quickstart (Docker)](#quickstart-docker)
  - [Endpoints \& UI](#endpoints--ui)
  - [Default credentials](#default-credentials)

## Features
- ASP.NET Core Web API with Swagger/OpenAPI
- SQL Server database with automatic migrations and initializer
- MinIO object storage for product images and uploads
- Background services for refresh token cleanup
- SignalR hub for notifications

## Architecture
The solution follows a layered architecture:
- API: ASP.NET Core Web API project (entry point)
- Core: DTOs, AutoMapper profiles, services and shared logic
- Data: Entity models and EF Core data access
- Tests: Unit tests

There are background services and a SignalR hub under `API/Hubs` and `API/Services`.

## Quickstart (Docker)
These instructions will bring up the full stack (recommended for first-time runs).

Prerequisites
- Docker Desktop (Windows) with WSL2 or Hyper-V enabled

Start services

```powershell
docker-compose up -d
```

Verify services

```powershell
docker-compose ps
```

Stop services

```powershell
docker-compose down
```

Rebuild and restart

```powershell
docker-compose up -d --build
```

Remove volumes (fresh start)

```powershell
docker-compose down -v
```

What the compose file starts
- SQL Server: listens on port 1433 inside the compose network (mapped to host via compose)
- Web API: mapped to http://localhost:5000
- MinIO: Console available on http://localhost:9001 and S3 endpoint on port 9000
- Adminer: DB UI on http://localhost:8080

## Endpoints & UI
- Web API: http://localhost:5000
- Swagger/OpenAPI: http://localhost:5000/swagger/index.html
- Adminer (DB UI): http://localhost:8080
  - System: MS SQL
  - Server: sqlserver
  - Username: sa
  - Password: YourStrong@Password123
  - Database: ceramic-display-db
- MinIO Console: http://localhost:9001
  - Username: admin
  - Password: password123

## Default credentials
- Email: admin@ceramic.local
- Password: Admin@123
- Username: Admin
