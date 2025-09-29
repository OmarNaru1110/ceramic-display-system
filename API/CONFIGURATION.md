# Configuration Guide

This application uses a hierarchical configuration system with the following priority order (highest to lowest):

## 1. Environment Variables (Highest Priority)
Environment variables will override all other configuration sources. Use the following format:

### Nested Configuration
For nested JSON properties, use double underscores (`__`) as separators:
```bash
# For JSON: { "ConnectionStrings": { "DefaultConnection": "value" } }
ConnectionStrings__DefaultConnection="your-connection-string"

# For JSON: { "Serilog": { "MinimumLevel": { "Default": "Debug" } } }
Serilog__MinimumLevel__Default=Debug
```

### Array Configuration
For arrays, use numeric indices:
```bash
# For JSON: { "Serilog": { "WriteTo": [{ "Name": "Console" }] } }
Serilog__WriteTo__0__Name=Console
```

## 2. appsettings.json (Medium Priority)
Base configuration file containing default settings for all environments.

## 3. appsettings.{Environment}.json (Lowest Priority)
Environment-specific configuration files:
- `appsettings.Development.json` - Development environment
- `appsettings.Production.json` - Production environment
- `appsettings.Staging.json` - Staging environment

## Common Configuration Scenarios

### Development
1. Use `appsettings.Development.json` for development-specific settings
2. Override sensitive values with environment variables or user secrets

### Production
1. Use environment variables for all sensitive data (connection strings, API keys, etc.)
2. Keep non-sensitive defaults in `appsettings.json`

### Example Environment Variables
See `.env.example` file for common configuration overrides.

## Setting Environment Variables

### Windows (PowerShell)
```powershell
$env:ConnectionStrings__DefaultConnection="your-connection-string"
```

### Windows (Command Prompt)
```cmd
set ConnectionStrings__DefaultConnection=your-connection-string
```

### Linux/macOS (Bash)
```bash
export ConnectionStrings__DefaultConnection="your-connection-string"
```

### Visual Studio / Development
Use the `launchSettings.json` file in the Properties folder to set environment variables for development.

## Configuration Validation
The application logs the configuration loading priority on startup to help with debugging configuration issues.