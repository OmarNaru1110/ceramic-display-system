using API.Configuration;
using API.Services;
using AutoMapper;
using CORE.DTOs.Auth;
using CORE.Services;
using CORE.Services.IServices;
using Data.DataAccess.Repositories.UnitOfWork;
using Data.Models;
using DATA.DataAccess.Context;
using DATA.DataAccess.Repositories;
using DATA.DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using System.Reflection;
using Microsoft.OpenApi.Models;

namespace API
{
    public class Program
    {
        public static async Task Main(string[] args)
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

                // Configure options
                builder.Services.Configure<DefaultAdminOptions>(
                    builder.Configuration.GetSection(DefaultAdminOptions.SectionName));

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

                // Configure Swagger to use JWT Bearer token
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "AggarAPI", Version = "v1" });

                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        In = ParameterLocation.Header,
                        Description = "Please enter JWT with Bearer into field",
                        Name = "Authorization",
                        Type = SecuritySchemeType.ApiKey,
                        BearerFormat = "JWT",
                        Scheme = "Bearer"
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement{
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                    }});
                });

                // Register DatabaseInitializer service
                builder.Services.AddScoped<DatabaseInitializer>();

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

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = true;
                    options.SaveToken = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidIssuer = builder.Configuration["JWT:Issuer"],
                        ValidAudience = builder.Configuration["JWT:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            // Read the token for SignalR connections
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/Chat"))
                            {
                                context.Token = accessToken;
                            }
                            // Read the token for SignalR connections
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/Notification"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };

                });
                builder.Services.AddAuthorization();

                // Register application services

                builder.Services.Configure<JwtConfig>(builder.Configuration.GetSection("JWT"));

                // Configure AutoMapper
                builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

                builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
                builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
                builder.Services.AddScoped<IAuthService, AuthService>();
                builder.Services.AddMemoryCache();
                builder.Services.AddOutputCache();

                var app = builder.Build();

                // Add Serilog request logging
                app.UseSerilogRequestLogging();

                // Configure the HTTP request pipeline.
                if (app.Environment.IsDevelopment())
                {
                    app.MapOpenApi();
                    app.UseDeveloperExceptionPage();
                }

                // Configure the HTTP request pipeline.
                app.UseSwagger();
                app.UseSwaggerUI();

                // Initialize database with roles and admin user
                using (var scope = app.Services.CreateScope())
                {
                    var databaseInitializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
                    try
                    {
                        Log.Information("Initializing database...");
                        await databaseInitializer.InitializeAsync();
                        Log.Information("Database initialized successfully");
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "An error occurred while initializing the database");
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
