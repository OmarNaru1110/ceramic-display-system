# Ceramic Display System - Database Initialization

This application is configured to automatically initialize the database with required roles and admin user when it starts up.

## What happens on startup:

1. **Database Creation**: The application ensures that the database exists and is created if it doesn't exist.
2. **Migrations**: Any pending Entity Framework migrations are automatically applied.
3. **Role Seeding**: All roles defined in the `UserRole` enum are created if they don't exist:
   - Admin
   - Seller
4. **Admin User Creation**: A default admin user is created if it doesn't exist.

## Configuration

### Default Admin User

The default admin user credentials can be configured in `appsettings.json` or environment-specific configuration files:

```json
{
  "DefaultAdmin": {
    "UserName": "admin",
    "Email": "admin@ceramicdisplay.com",
    "Password": "Admin@123"
  }
}
```

### Environment-Specific Configuration

Different environments can have different admin configurations:

- **Production**: Configure in `appsettings.json` or environment variables
- **Development**: Configure in `appsettings.Development.json`
- **Environment Variables**: Use the following format:
  - `DefaultAdmin__UserName`
  - `DefaultAdmin__Email`
  - `DefaultAdmin__Password`

### Database Connection

Configure your database connection string in the appropriate configuration file:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your connection string here"
  }
}
```

## First Time Setup

1. **Clone the repository**
2. **Update connection strings** in `appsettings.json` and/or `appsettings.Development.json`
3. **Optionally update admin credentials** in the configuration files
4. **Run the application**: `dotnet run --project API`

The application will automatically:
- Create the database if it doesn't exist
- Apply all migrations
- Create the required roles
- Create the admin user with the configured credentials

## Security Notes

- **Change default passwords**: Make sure to change the default admin password in production
- **Use environment variables**: For production, consider using environment variables or Azure Key Vault for sensitive configuration
- **Password requirements**: The default password policy requires:
  - At least 6 characters
  - At least one digit
  - At least one lowercase letter
  - At least one uppercase letter
  - Non-alphanumeric characters are optional

## Logging

The application logs all database initialization activities. Check the console output or log files in the `logs/` directory for detailed information about the initialization process.

## Troubleshooting

If the database initialization fails:

1. Check the connection string is correct
2. Ensure the database server is running and accessible
3. Check the logs for detailed error messages
4. Verify the admin configuration is complete and valid
5. Ensure the application has sufficient permissions to create databases and users