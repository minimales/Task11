using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Polly;
using Polly.Retry;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using FluentValidation;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Extensions;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Enums;
using SharpGrip.FluentValidation.AutoValidation.Mvc.Results;
using task11.ApplicationCore;
using task11.ApplicationCore.Auth;
using task11.ApplicationCore.Currency;
using task11.ApplicationCore.Repositories.Abstractions;
using task11.ApplicationCore.Services;
using task11.ApplicationCore.Services.Abstractions;
using task11.ApplicationCore.Validators;
using task11.ApplicationCore.Entities;
using task11.Infrastructure.Currency;
using task11.Infrastructure.Persistence;
using task11.Infrastructure.Repositories;
using task11.Infrastructure.Time;
using task11.Web.Infrastructure;
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
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    ConfigurationManager config = builder.Configuration;

    string connectionString = config.GetConnectionString("Default")
        ?? "Host=localhost;Port=5432;Database=finance;Username=finance;Password=finance";
    bool useInMemory = config.GetValue<bool>("UseInMemory");

    builder.Services.AddSingleton<IClock, SystemClock>();
    builder.Services.AddSingleton(sp =>
        new DbContextFactory(connectionString, useInMemory, sp.GetRequiredService<IClock>()));

    JwtSettings jwtSettings = new JwtSettings();
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

    SymmetricSecurityKey signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret));

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

    FxOptions fxOptions = new FxOptions();
    config.GetSection(FxOptions.SectionName).Bind(fxOptions);
    int fxRetryCount = fxOptions.RetryCount > 0 ? fxOptions.RetryCount : 3;
    const string FxUserAgent = "task11-PersonalFinanceApi/1.0";

    builder.Services.AddHttpClient<FrankfurterClient>((sp, client) =>
        {
            FxOptions options = sp.GetRequiredService<IOptions<FxOptions>>().Value;
            string baseUrl = string.IsNullOrWhiteSpace(options.BaseUrl)
                ? "https://api.frankfurter.dev"
                : options.BaseUrl;

            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            // Let AddStandardResilienceHandler own timeouts (10s attempt / 30s total);
            // a HttpClient.Timeout here would cap the whole pipeline and starve retries.
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(FxUserAgent);
        })
        .AddStandardResilienceHandler(resilience =>
        {
            resilience.Retry.MaxRetryAttempts = fxRetryCount;
        });

    builder.Services.AddHttpClient<PrivatBankClient>((sp, client) =>
        {
            FxOptions options = sp.GetRequiredService<IOptions<FxOptions>>().Value;
            string baseUrl = string.IsNullOrWhiteSpace(options.PrivatBankBaseUrl)
                ? "https://api.privatbank.ua"
                : options.PrivatBankBaseUrl;

            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            // Let AddStandardResilienceHandler own timeouts (10s attempt / 30s total);
            // a HttpClient.Timeout here would cap the whole pipeline and starve retries.
            client.Timeout = Timeout.InfiniteTimeSpan;
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(FxUserAgent);
        })
        .AddStandardResilienceHandler(resilience =>
        {
            resilience.Retry.MaxRetryAttempts = fxRetryCount;
        });

    builder.Services.AddScoped<ICurrencyConverter, CurrencyConverter>();

    builder.Services.AddValidatorsFromAssemblyContaining<LoginModelValidator>(includeInternalTypes: true);
    builder.Services.AddFluentValidationAutoValidation(c =>
    {
        c.DisableBuiltInModelValidation = true;
        c.ValidationStrategy = ValidationStrategy.All;
    });
    builder.Services.AddScoped<IFluentValidationAutoValidationResultFactory, ValidationProblemResultFactory>();

    builder.Services.AddProblemDetails(options =>
    {
        options.CustomizeProblemDetails = context =>
        {
            context.ProblemDetails.Type ??= "about:blank";
            ProblemDetailsEnricher.Enrich(context.HttpContext, context.ProblemDetails);
        };
    });

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

    // Unify the validation 400 body with the exception-mapped ProblemDetails contract.
    // SharpGrip auto-validation and built-in [ApiController] validation both route
    // through ApiBehaviorOptions.InvalidModelStateResponseFactory.
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            ValidationProblemDetails problemDetails = new ValidationProblemDetails(context.ModelState)
            {
                Type = "about:blank",
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Instance = context.HttpContext.Request.Path
            };

            ProblemDetailsEnricher.Enrich(context.HttpContext, problemDetails);

            return new BadRequestObjectResult(problemDetails)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Personal Finance API",
            Version = "v1",
            Description = "Personal finance management API with wallets, operations and reports."
        });

        OpenApiSecurityScheme scheme = new OpenApiSecurityScheme
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

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            // Don't echo internal CLR/model type names from deserialization exceptions to clients.
            options.AllowInputFormatterExceptionMessages = false;
        });

    WebApplication app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    // Wraps the whole pipeline so error (4xx/5xx) responses are logged with their final status+body.
    app.UseMiddleware<RequestResponseLoggingMiddleware>();

    // IExceptionHandler-based ProblemDetails pipeline (replaces the old custom middleware).
    app.UseExceptionHandler();
    // Gives empty-body challenge responses (401/403/404/405) a ProblemDetails body too.
    app.UseStatusCodePages();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseAuthentication();
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

static async Task MigrateAndSeedAsync(WebApplication app)
{
    ILogger<Program> logger = app.Services.GetRequiredService<ILogger<Program>>();
    DbContextFactory factory = app.Services.GetRequiredService<DbContextFactory>();

    ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(),
            MaxRetryAttempts = 10,
            BackoffType = DelayBackoffType.Exponential,
            UseJitter = true,
            Delay = TimeSpan.FromSeconds(1),
            MaxDelay = TimeSpan.FromSeconds(30),
            OnRetry = args =>
            {
                logger.LogWarning(
                    args.Outcome.Exception,
                    "Database migration attempt {Attempt} failed; retrying in {Delay}.",
                    args.AttemptNumber + 1, args.RetryDelay);
                return ValueTask.CompletedTask;
            }
        })
        .Build();

    await pipeline.ExecuteAsync(async _ =>
    {
        factory.MigrateIfRelational();
        // Seeding is idempotent (guarded by AnyAsync(Users)); run it inside the same
        // retry pipeline so a transient fault is retried instead of crashing startup.
        await SeedAsync(app.Services);
    });
}

static async Task SeedAsync(IServiceProvider services)
{
    DbContextFactory factory = services.GetRequiredService<DbContextFactory>();
    PasswordHasher hasher = services.GetRequiredService<PasswordHasher>();
    IClock clock = services.GetRequiredService<IClock>();
    IConfiguration configuration = services.GetRequiredService<IConfiguration>();
    ILogger<Program> logger = services.GetRequiredService<ILogger<Program>>();

    await using AppDbContext db = factory.CreateDbContext();

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

    UserEntity admin = new UserEntity
    {
        Username = adminUsername,
        PasswordHash = hasher.Hash(adminPassword),
        Role = "Admin",
        CreatedAtUtc = now
    };

    WalletEntity sharedWallet = new WalletEntity
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
