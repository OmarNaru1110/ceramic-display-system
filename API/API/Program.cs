using DATA.DataAccess.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Data.Models;
using Serilog;

namespace API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create initial logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console()
                .CreateBootstrapLogger();

            try
            {
                Log.Information("Starting web application");

                var builder = WebApplication.CreateBuilder(args);

                // Clear existing configuration sources and rebuild in the correct order
                builder.Configuration.Sources.Clear();
                
                // Build configuration in the desired priority order:
                // 1. Environment Variables (highest priority)
                // 2. appsettings.json
                // 3. appsettings.{Environment}.json (lowest priority)
                builder.Configuration
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", 
                        optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .AddCommandLine(args);

                Log.Information("Configuration loaded with priority: Environment Variables > appsettings.json > appsettings.{Environment}.json");

                // Configure Serilog
                builder.Host.UseSerilog((context, services, configuration) =>
                    configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .ReadFrom.Services(services)
                        .Enrich.FromLogContext()
                        .WriteTo.Console()
                        .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                );

                // Configure Entity Framework with SQL Server
                builder.Services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"), builder =>
                    {
                        builder.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
                    });
                    
                    // Enable detailed errors in development
                    if (builder.Environment.IsDevelopment())
                    {
                        options.EnableDetailedErrors();
                        options.EnableSensitiveDataLogging();
                    }
                });

                // Configure ASP.NET Core Identity
                builder.Services.AddIdentity<AppUser, IdentityRole<int>>(options =>
                {
                    // Password settings
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = true;
                    options.Password.RequiredLength = 6;
                    options.Password.RequiredUniqueChars = 1;

                    // Lockout settings
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;

                    // User settings
                    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                    options.User.RequireUniqueEmail = true;

                    // Sign-in settings
                    options.SignIn.RequireConfirmedEmail = false;
                    options.SignIn.RequireConfirmedPhoneNumber = false;
                })
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

                // Add services to the container.
                builder.Services.AddControllers();
                
                // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
                builder.Services.AddOpenApi();

                // Add CORS
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowAll", policy =>
                    {
                        policy
                            .AllowAnyOrigin()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
                });

                var app = builder.Build();

                // Add Serilog request logging
                app.UseSerilogRequestLogging();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                    app.UseDeveloperExceptionPage();
                }

                // Apply database migrations automatically
                using (var scope = app.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    try
                    {
                        Log.Information("Applying database migrations...");
                        dbContext.Database.Migrate();
                        Log.Information("Database migrations applied successfully");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "An error occurred while applying database migrations");
                        throw;
                    }
                }

                app.UseHttpsRedirection();
                app.UseCors("AllowAll");
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapControllers();

                Log.Information("Web application started successfully");
                app.Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
