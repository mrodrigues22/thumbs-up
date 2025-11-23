using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ThumbsUpApi.Data;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using ThumbsUpApi.Models;
using ThumbsUpApi.Services;
using ThumbsUpApi.Configuration;
using ThumbsUpApi.Interfaces;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Add Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

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
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

// Configure Options with validation
builder.Services.Configure<AiOptions>(builder.Configuration.GetSection("Ai:OpenAi"));
builder.Services.Configure<AiPredictorOptions>(builder.Configuration.GetSection("Ai:Predictor"));
builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));

// Validate options on startup
builder.Services.AddOptionsWithValidateOnStart<AiOptions>();
builder.Services.AddOptionsWithValidateOnStart<AiPredictorOptions>();
builder.Services.AddOptionsWithValidateOnStart<FileStorageOptions>();

// HttpClient factory with Polly retry policies
builder.Services.AddHttpClient();

// Configure OpenAI HTTP client with retry policy
builder.Services.AddHttpClient("OpenAiClient")
    .AddPolicyHandler((serviceProvider, request) => HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                var logger = serviceProvider.GetService<ILogger<Program>>();
                logger?.LogWarning("OpenAI API retry attempt {RetryAttempt} after {Delay}ms due to: {Exception}",
                    retryAttempt, timespan.TotalMilliseconds, outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
            }));

// AI client
builder.Services.AddScoped<ThumbsUpApi.Services.IOpenAiClient, ThumbsUpApi.Services.OpenAiClient>();

// Add HttpContextAccessor for accessing request information
builder.Services.AddHttpContextAccessor();

// Add Repositories
builder.Services.AddScoped<ThumbsUpApi.Repositories.ISubmissionRepository, ThumbsUpApi.Repositories.SubmissionRepository>();
builder.Services.AddScoped<ThumbsUpApi.Repositories.IReviewRepository, ThumbsUpApi.Repositories.ReviewRepository>();
builder.Services.AddScoped<ThumbsUpApi.Repositories.IUserRepository, ThumbsUpApi.Repositories.UserRepository>();
builder.Services.AddScoped<ThumbsUpApi.Repositories.IClientRepository, ThumbsUpApi.Repositories.ClientRepository>();
builder.Services.AddScoped<ThumbsUpApi.Repositories.IContentFeatureRepository, ThumbsUpApi.Repositories.ContentFeatureRepository>();
builder.Services.AddScoped<ThumbsUpApi.Repositories.IClientSummaryRepository, ThumbsUpApi.Repositories.ClientSummaryRepository>();

// Add Mappers
builder.Services.AddScoped<ThumbsUpApi.Mappers.SubmissionMapper>();
builder.Services.AddScoped<ThumbsUpApi.Mappers.ReviewMapper>();

// Add Services
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IImageCompressionService, ImageCompressionService>();
// AI services (GPT-based implementations only)
builder.Services.AddScoped<ThumbsUpApi.Services.IImageOcrService, ThumbsUpApi.Services.OpenAiOcrService>();
builder.Services.AddScoped<ThumbsUpApi.Services.IImageThemeService, ThumbsUpApi.Services.OpenAiThemeService>();
builder.Services.AddScoped<ThumbsUpApi.Services.ITextGenerationService, ThumbsUpApi.Services.OpenAiTextService>();
builder.Services.AddScoped<ThumbsUpApi.Services.IApprovalPredictor, ThumbsUpApi.Services.HybridApprovalPredictor>();
// Orchestration services with interfaces
builder.Services.AddScoped<ThumbsUpApi.Interfaces.IImageAnalysisService, ThumbsUpApi.Services.ImageAnalysisService>();
builder.Services.AddScoped<ThumbsUpApi.Interfaces.IReviewPredictorService, ThumbsUpApi.Services.ReviewPredictorService>();

// Content Summary service (conditional AI enhancement)
builder.Services.AddScoped<RuleBasedContentSummaryService>();
if (builder.Configuration.GetValue<bool>("Submission:EnableAiSummary", true))
{
    builder.Services.AddScoped<IContentSummaryService, AiContentSummaryService>();
}
else
{
    builder.Services.AddScoped<IContentSummaryService, RuleBasedContentSummaryService>();
}

// Queue and background worker
builder.Services.AddSingleton<ThumbsUpApi.Services.ISubmissionAnalysisQueue, ThumbsUpApi.Services.SubmissionAnalysisQueue>();
builder.Services.AddHostedService<ThumbsUpApi.Services.AiProcessingWorker>();
builder.Services.AddHostedService<ThumbsUpApi.Services.AnalysisBackfillWorker>();

// Rate Limiting for AI endpoints
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("ai", httpContext =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: httpContext.User?.Identity?.Name ?? "anon",
            factory: key => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 20,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                TokensPerPeriod = 20,
                AutoReplenishment = true
            }));
});

// Add Controllers
builder.Services.AddControllers();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" })
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ThumbsUp API",
        Version = "v1",
        Description = "API for ThumbsUp submission and review system"
    });

    // Add JWT Bearer authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});

var app = builder.Build();

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseRateLimiter();

// Serve static files from wwwroot
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
