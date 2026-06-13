using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using FluentValidation;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using task11.ApplicationCore;
using task11.ApplicationCore.Auth;
using task11.ApplicationCore.Currency;
using task11.ApplicationCore.Repositories;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.ApplicationCore.Services;
using task11.ApplicationCore.Services.Abstractions;
using task11.ApplicationCore.Validators;
using task11.Data;
using task11.Data.Entities;
using task11.Web.Infrastructure.Auth;
using task11.Web.Middleware;

ConfigureSerilog(new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build());

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    var config = builder.Configuration;

    string connectionString = config.GetConnectionString("Default")
        ?? "Host=localhost;Port=5432;Database=finance;Username=finance;Password=finance";
    bool useInMemory = config.GetValue<bool>("UseInMemory");

    builder.Services.AddSingleton<IClock, SystemClock>();
    builder.Services.AddSingleton(sp =>
        new DbContextFactory(connectionString, useInMemory, sp.GetRequiredService<IClock>()));

    var jwtSettings = new JwtSettings();
    config.GetSection(JwtSettings.SectionName).Bind(jwtSettings);

    if (string.IsNullOrWhiteSpace(jwtSettings.Secret) || jwtSettings.Secret.Length < 32)
    {
        throw new InvalidOperationException(
            "Jwt:Secret must be configured and at least 32 characters long for HS256.");
    }

    builder.Services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));
    builder.Services.AddSingleton<PasswordHasher>();
    builder.Services.AddSingleton<JwtTokenGenerator>();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, CurrentUser>();

    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret));

    builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = signingKey,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = "name",
                RoleClaimType = "role"
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
    });

    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IWalletRepository, WalletRepository>();
    builder.Services.AddScoped<IOperationTypeRepository, OperationTypeRepository>();
    builder.Services.AddScoped<IOperationRepository, OperationRepository>();
    builder.Services.AddScoped<IReportRepository, ReportRepository>();

    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IWalletService, WalletService>();
    builder.Services.AddScoped<IOperationTypeService, OperationTypeService>();
    builder.Services.AddScoped<IOperationService, OperationService>();
    builder.Services.AddScoped<IReportService, ReportService>();

    builder.Services.AddOptions<FxOptions>()
        .BindConfiguration(FxOptions.SectionName)
        .ValidateOnStart();

    builder.Services.AddMemoryCache();

    builder.Services.AddHttpClient<FrankfurterClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<FxOptions>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                ? "https://api.frankfurter.dev"
                : options.BaseUrl;

            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        })
        .AddPolicyHandler((sp, _) =>
            FxRetryPolicy(sp.GetRequiredService<IOptions<FxOptions>>().Value.RetryCount));

    builder.Services.AddHttpClient<PrivatBankClient>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<FxOptions>>().Value;
            var baseUrl = string.IsNullOrWhiteSpace(options.PrivatBankBaseUrl)
                ? "https://api.privatbank.ua"
                : options.PrivatBankBaseUrl;

            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        })
        .AddPolicyHandler((sp, _) =>
            FxRetryPolicy(sp.GetRequiredService<IOptions<FxOptions>>().Value.RetryCount));

    builder.Services.AddScoped<ICurrencyConverter, CurrencyConverter>();

    builder.Services.AddValidatorsFromAssemblyContaining<LoginModelValidator>(includeInternalTypes: true);
    builder.Services.AddFluentValidationAutoValidation(c => c.DisableBuiltInModelValidation = true);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Personal Finance API",
            Version = "v1",
            Description = "Personal finance management API with wallets, operations and reports."
        });

        var scheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Enter the JWT as: Bearer {token}",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        };

        options.AddSecurityDefinition("Bearer", scheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            [scheme] = Array.Empty<string>()
        });
    });

    builder.Services.AddControllers();

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseAuthentication();
    app.UseMiddleware<RequestResponseLoggingMiddleware>();
    app.UseAuthorization();

    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
        .AllowAnonymous()
        .WithName("Health");

    await MigrateAndSeedAsync(app);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly during startup.");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

static void ConfigureSerilog(IConfiguration configuration)
{
    string minimumLevel = configuration["Serilog:MinimumLevel"] ?? "Information";
    if (!Enum.TryParse(minimumLevel, ignoreCase: true, out LogEventLevel level))
    {
        level = LogEventLevel.Information;
    }

    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Is(level)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console(new JsonFormatter(renderMessage: true))
        .CreateLogger();
}

static IAsyncPolicy<HttpResponseMessage> FxRetryPolicy(int retryCount) =>
    HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: retryCount > 0 ? retryCount : 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));

static async Task MigrateAndSeedAsync(WebApplication app)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    var factory = app.Services.GetRequiredService<DbContextFactory>();

    var retryPolicy = Policy
        .Handle<Exception>()
        .WaitAndRetryAsync(
            retryCount: 10,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))),
            onRetry: (exception, delay, attempt, _) =>
                logger.LogWarning(
                    exception,
                    "Database migration attempt {Attempt} failed; retrying in {Delay}.",
                    attempt, delay));

    await retryPolicy.ExecuteAsync(() =>
    {
        factory.MigrateIfRelational();
        return Task.CompletedTask;
    });

    await SeedAsync(app.Services);
}

static async Task SeedAsync(IServiceProvider services)
{
    var factory = services.GetRequiredService<DbContextFactory>();
    var hasher = services.GetRequiredService<PasswordHasher>();
    var clock = services.GetRequiredService<IClock>();
    var configuration = services.GetRequiredService<IConfiguration>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    await using var db = factory.CreateDbContext();

    if (await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .AnyAsync(db.Users))
    {
        logger.LogInformation("Seed skipped: users already present.");
        return;
    }

    string adminUsername = configuration["Seed:AdminUsername"] ?? "admin";
    string adminPassword = configuration["Seed:AdminPassword"] ?? "admin";
    string defaultCurrency = configuration["Seed:DefaultWalletCurrency"] ?? "UAH";

    DateTime now = clock.UtcNow;

    var admin = new UserEntity
    {
        Username = adminUsername,
        PasswordHash = hasher.Hash(adminPassword),
        Role = "Admin",
        CreatedAtUtc = now
    };

    var sharedWallet = new WalletEntity
    {
        Name = "Shared",
        BaseCurrency = defaultCurrency,
        OwnerUserId = null,
        CreatedAtUtc = now
    };

    await db.Users.AddAsync(admin);
    await db.Wallets.AddAsync(sharedWallet);
    await db.SaveChangesAsync();

    logger.LogInformation(
        "Seeded default admin user '{Username}' and shared wallet '{Wallet}'.",
        adminUsername, sharedWallet.Name);
}

public partial class Program { }
