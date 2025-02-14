using Microsoft.AspNetCore.Authentication.JwtBearer;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Serilog;
using System.Text;
using Polly;
using Polly.Extensions.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;



var builder = WebApplication.CreateBuilder(args);

// Caching: Redis Setup
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "CurrencyCache_";
});
builder.Services.AddSingleton<RedisCacheService>();


// Register Providers
builder.Services.AddScoped<FrankfurterExchangeRateProvider>();
builder.Services.AddScoped<IExchangeRateProvider, FrankfurterExchangeRateProvider>();

// HTTP Client & Resilience: Polly Policies
builder.Services.AddHttpClient<FrankfurterExchangeRateProvider>()
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(2)))
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(3, TimeSpan.FromSeconds(30)));

// Add Rate Limiting Policy
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("StrictPolicy", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "global",
            key => new FixedWindowRateLimiterOptions // Ensure key is passed
            {
                PermitLimit = 10,  // Allow 10 requests
                Window = TimeSpan.FromMinutes(1), // Per minute
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 2
            }
        )
    );
});

// Application Services
builder.Services.AddScoped<ExchangeRateProviderFactory>();
builder.Services.AddSingleton<FrankfurterExchangeRateProvider>();
builder.Services.AddScoped<IExchangeRateProvider, FrankfurterExchangeRateProvider>();

// Authentication & Authorization
var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT Key is missing in the configuration.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Configure API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.ApiVersionReader = new UrlSegmentApiVersionReader(); // or HeaderApiVersionReader()
});


// Add OpenTelemetry for distributed tracing
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CurrencyConverterAPI"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317")); // Replace with your OTEL Collector
    });

// Swagger (API Documentation)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Controllers
builder.Services.AddControllers();
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services));

// Set up Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.Seq("http://localhost:5341") // Replace with your Seq server URL
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole(); // Use Console Logging 
});
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseMiddleware<LoggingMiddleware>(); 
app.UseSerilogRequestLogging(); 
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseRateLimiter();// Enable Rate Limiting
app.Run();
