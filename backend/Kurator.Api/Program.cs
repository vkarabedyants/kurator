using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.DataProtection;
using System.Text;
using System.Diagnostics;
using Serilog;
using Serilog.Events;
using Kurator.Infrastructure.Data;
using Kurator.Core.Interfaces;
using Kurator.Infrastructure.Services;
using Kurator.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog with maximum detail for stdout
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithEnvironmentName()
    .Enrich.WithProperty("Application", "Kurator.Api")
    .Enrich.WithProperty("Version", typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level:u3}] [{SourceContext}] [ReqId:{RequestId}] [User:{UserId}] [CorrelationId:{CorrelationId}] {Message:lj}{NewLine}{Exception}",
        restrictedToMinimumLevel: LogEventLevel.Verbose)
    .CreateLogger();

builder.Host.UseSerilog();

// Log application startup
Log.Information("=== KURATOR API Starting ===");
Log.Information("Environment: {Environment}", builder.Environment.EnvironmentName);
Log.Information("Content Root: {ContentRoot}", builder.Environment.ContentRootPath);
Log.Information("Application Name: {ApplicationName}", builder.Environment.ApplicationName);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Kurator API",
        Version = "v1",
        Description = "API for KURATOR governance relationships management system"
    });

    // Include XML comments for better documentation
    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Database with detailed logging
Log.Information("Configuring database connection...");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    Log.Debug("Connection string configured (masked): {ConnectionStringMasked}",
        connectionString?.Split(';').FirstOrDefault() ?? "Not configured");

    if (builder.Environment.IsDevelopment() && connectionString?.Contains(".db") == true)
    {
        Log.Information("Using SQLite database provider for development");
        options.UseSqlite(connectionString);
        options.EnableSensitiveDataLogging(); // Log parameter values in development
        options.EnableDetailedErrors(); // More detailed error messages
    }
    else
    {
        Log.Information("Using PostgreSQL database provider");
        options.UseNpgsql(connectionString);

        // Only enable sensitive data logging and detailed errors in development
        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging(); // Log parameter values in development
            options.EnableDetailedErrors(); // More detailed error messages
        }
    }

    // Log all database operations
    options.LogTo(
        message => Log.Debug("[EF Core] {Message}", message),
        new[] { DbLoggerCategory.Database.Command.Name },
        LogLevel.Debug);
});

// Configure Data Protection
var dataProtectionBuilder = builder.Services.AddDataProtection()
    .SetApplicationName("Kurator");

// Configure key persistence location
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrEmpty(keysPath))
{
    // For Docker/production with volume mount
    Directory.CreateDirectory(keysPath); // Ensure directory exists
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));

    // Log configuration
    Log.Information("DataProtection keys configured to persist at: {KeysPath}", keysPath);
}
else if (builder.Environment.IsDevelopment())
{
    // For local development
    var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    var keysDirectory = Path.Combine(userProfile, "Kurator", "DataProtection-Keys");
    Directory.CreateDirectory(keysDirectory);
    dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysDirectory));

    // Log configuration
    Log.Information("DataProtection keys configured for development at: {KeysDirectory}", keysDirectory);
}

// Configure key protection with certificate if available
var certificatePath = builder.Configuration["DataProtection:CertificatePath"];
var certificatePassword = builder.Configuration["DataProtection:CertificatePassword"];
if (!string.IsNullOrEmpty(certificatePath) && File.Exists(certificatePath))
{
    try
    {
        var certificate = System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(
            certificatePath,
            certificatePassword);
        dataProtectionBuilder.ProtectKeysWithCertificate(certificate);
        Log.Information("DataProtection keys protected with certificate from: {CertificatePath}", certificatePath);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Failed to load certificate for DataProtection from {CertificatePath}, using default protection", certificatePath);
    }
}

// Services registration with logging
Log.Information("Registering application services...");
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Kurator.Infrastructure.Repositories.Repository<>));
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IInteractionService, InteractionService>();
builder.Services.AddScoped<IWatchlistService, WatchlistService>();
builder.Services.AddSingleton<TotpService>();
Log.Debug("All application services registered successfully");

// JWT Authentication with detailed logging
Log.Information("Configuring JWT Authentication...");
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
Log.Debug("JWT Issuer: {Issuer}, Audience: {Audience}, ExpiryMinutes: {ExpiryMinutes}",
    jwtSettings["Issuer"], jwtSettings["Audience"], jwtSettings["ExpiryMinutes"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };

    // Add JWT authentication events for detailed logging
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Log.Warning("JWT Authentication failed: {Error}, Exception: {ExceptionType}",
                context.Exception.Message, context.Exception.GetType().Name);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var userId = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var role = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            Log.Debug("JWT Token validated successfully. UserId: {UserId}, UserName: {UserName}, Role: {Role}",
                userId, userName, role);
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            Log.Warning("JWT Challenge issued. Error: {Error}, ErrorDescription: {ErrorDescription}",
                context.Error, context.ErrorDescription);
            return Task.CompletedTask;
        },
        OnForbidden = context =>
        {
            Log.Warning("JWT Forbidden. User attempted to access forbidden resource");
            return Task.CompletedTask;
        },
        OnMessageReceived = context =>
        {
            var hasToken = !string.IsNullOrEmpty(context.Token);
            Log.Verbose("JWT Message received. Token present: {HasToken}", hasToken);
            return Task.CompletedTask;
        }
    };
});
Log.Information("JWT Authentication configured successfully");

builder.Services.AddAuthorization();

// CORS configuration
var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>();
if (corsOrigins == null || corsOrigins.Length == 0)
{
    // Try to get from environment variable (comma-separated)
    var envCorsOrigins = Environment.GetEnvironmentVariable("CorsOrigins");
    corsOrigins = !string.IsNullOrEmpty(envCorsOrigins)
        ? envCorsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        : new[] { "http://localhost:3000" };
}

Log.Information("CORS Origins configured: {Origins}", string.Join(", ", corsOrigins));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetIsOriginAllowedToAllowWildcardSubdomains();
    });
});

var app = builder.Build();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    try
    {
        await Kurator.Infrastructure.DbInitializer.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline
Log.Information("Configuring HTTP request pipeline...");

app.UseExceptionHandling();

// Swagger always enabled for API documentation and client generation
app.UseSwagger();
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Kurator API v1");
        c.RoutePrefix = "swagger";
    });
    Log.Information("Swagger UI enabled at /swagger");
}

// Add detailed request/response logging middleware
app.UseRequestResponseLogging();

// Serilog request logging with enrichment
app.UseSerilogRequestLogging(options =>
{
    // Customize the message template
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

    // Emit debug-level logs for successful requests
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex != null) return LogEventLevel.Error;
        if (httpContext.Response.StatusCode >= 500) return LogEventLevel.Error;
        if (httpContext.Response.StatusCode >= 400) return LogEventLevel.Warning;
        return LogEventLevel.Information;
    };

    // Attach additional properties to the request completion event
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("RequestProtocol", httpContext.Request.Protocol);
        diagnosticContext.Set("QueryString", httpContext.Request.QueryString.Value);
        diagnosticContext.Set("ContentType", httpContext.Request.ContentType);
        diagnosticContext.Set("ContentLength", httpContext.Request.ContentLength);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        diagnosticContext.Set("ClientIP", httpContext.Connection.RemoteIpAddress?.ToString());

        // Add user information if authenticated
        if (httpContext.User.Identity?.IsAuthenticated == true)
        {
            var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var role = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            diagnosticContext.Set("UserId", userId);
            diagnosticContext.Set("UserName", userName);
            diagnosticContext.Set("UserRole", role);
        }

        // Add response information
        diagnosticContext.Set("ResponseContentType", httpContext.Response.ContentType);
        diagnosticContext.Set("ResponseContentLength", httpContext.Response.ContentLength);
    };
});

app.UseCors();
Log.Debug("CORS middleware enabled");

app.UseAuthentication();
Log.Debug("Authentication middleware enabled");

app.UseAuthorization();
Log.Debug("Authorization middleware enabled");

app.MapControllers();
Log.Information("Controllers mapped successfully");

app.MapGet("/health", () =>
{
    Log.Debug("Health check endpoint called");
    return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
});

Log.Information("=== KURATOR API Started Successfully ===");
Log.Information("Listening on: {Urls}", string.Join(", ", app.Urls));

app.Run();
