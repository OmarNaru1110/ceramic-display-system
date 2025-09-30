using DATA.DataAccess.Context;
using Data.Models;
using Data.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using API.Configuration;

namespace API.Services
{
    public class DatabaseInitializer
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly ILogger<DatabaseInitializer> _logger;
        private readonly DefaultAdminOptions _adminOptions;

        public DatabaseInitializer(
            AppDbContext context,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole<int>> roleManager,
            ILogger<DatabaseInitializer> logger,
            IOptions<DefaultAdminOptions> adminOptions)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _adminOptions = adminOptions.Value;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Starting database initialization...");

                // Check if database exists
                var canConnect = await _context.Database.CanConnectAsync();
                
                if (!canConnect)
                {
                    _logger.LogInformation("Database does not exist. Creating database...");
                }

                // Apply migrations (this will create the database if it doesn't exist)
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    _logger.LogInformation("Applying {Count} pending migrations: {Migrations}", 
                        pendingMigrations.Count(), string.Join(", ", pendingMigrations));
                    await _context.Database.MigrateAsync();
                    _logger.LogInformation("Migrations applied successfully");
                }
                else
                {
                    _logger.LogInformation("No pending migrations found");
                    
                    // Ensure database is created even if no migrations are pending
                    await _context.Database.EnsureCreatedAsync();
                }

                // Seed roles
                await SeedRolesAsync();

                // Seed admin user
                await SeedAdminUserAsync();

                _logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during database initialization");
                throw;
            }
        }

        private async Task SeedRolesAsync()
        {
            _logger.LogInformation("Seeding roles...");

            var roles = Enum.GetNames<UserRole>();

            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    _logger.LogInformation("Creating role: {RoleName}", roleName);
                    var role = new IdentityRole<int>(roleName);
                    var result = await _roleManager.CreateAsync(role);
                    
                    if (!result.Succeeded)
                    {
                        _logger.LogError("Failed to create role {RoleName}: {Errors}", 
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                    else
                    {
                        _logger.LogInformation("Role {RoleName} created successfully", roleName);
                    }
                }
                else
                {
                    _logger.LogInformation("Role {RoleName} already exists", roleName);
                }
            }
        }

        private async Task SeedAdminUserAsync()
        {
            _logger.LogInformation("Seeding admin user...");

            var adminEmail = _adminOptions.Email;
            var adminUserName = _adminOptions.UserName;
            var adminPassword = _adminOptions.Password;

            if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminUserName) || string.IsNullOrEmpty(adminPassword))
            {
                _logger.LogWarning("Admin configuration is incomplete. Skipping admin user creation.");
                return;
            }

            var adminUser = await _userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                _logger.LogInformation("Creating admin user with email: {AdminEmail}", adminEmail);
                
                adminUser = new AppUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(adminUser, adminPassword);
                
                if (result.Succeeded)
                {
                    _logger.LogInformation("Admin user created successfully");
                    
                    // Add admin role to the user
                    var roleResult = await _userManager.AddToRoleAsync(adminUser, UserRole.Admin.ToString());
                    
                    if (roleResult.Succeeded)
                    {
                        _logger.LogInformation("Admin role assigned to admin user successfully");
                    }
                    else
                    {
                        _logger.LogError("Failed to assign admin role: {Errors}", 
                            string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    _logger.LogError("Failed to create admin user: {Errors}", 
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                _logger.LogInformation("Admin user already exists with email: {AdminEmail}", adminEmail);
                
                // Ensure admin user has admin role
                if (!await _userManager.IsInRoleAsync(adminUser, UserRole.Admin.ToString()))
                {
                    _logger.LogInformation("Adding admin role to existing admin user...");
                    var roleResult = await _userManager.AddToRoleAsync(adminUser, UserRole.Admin.ToString());
                    
                    if (roleResult.Succeeded)
                    {
                        _logger.LogInformation("Admin role assigned to existing user successfully");
                    }
                    else
                    {
                        _logger.LogError("Failed to assign admin role to existing user: {Errors}", 
                            string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    _logger.LogInformation("Admin user already has admin role");
                }
            }
        }
    }
}