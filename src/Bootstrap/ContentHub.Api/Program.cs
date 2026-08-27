using System.Threading.RateLimiting;
using ContentHub.Api.Infrastructure;
using ContentHub.Api.Modules;
using ContentHub.Api.Security;
using ContentHub.BuildingBlocks.Application.DependencyInjection;
using ContentHub.BuildingBlocks.Infrastructure.DependencyInjection;
using ContentHub.Modules.ContentSearch.Endpoints;
using ContentHub.Modules.ContentSearch.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// --- Modülleri keşfet (modüler monolit) ---
var modules = ModuleRegistrar.DiscoverModules(typeof(ContentSearchModule).Assembly);

// --- BuildingBlocks (bir kez) ---
builder.Services.AddBuildingBlocksApplication();     // MediatR pipeline behaviors (validation + logging)
builder.Services.AddBuildingBlocksInfrastructure();  // IClock -> SystemClock

// --- Modül Application/Endpoints kaydı ---
foreach (var module in modules)
{
    module.RegisterServices(builder.Services, builder.Configuration);
}

// --- Modül Infrastructure kaydı (composition root) ---
builder.Services.AddContentSearchInfrastructure(builder.Configuration);

// --- Dağıtık önbellek: Redis varsa onu, yoksa in-memory (canlı ücretsiz katman) ---
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// --- Güvenlik: ApiKey şeması + politika (yalnız yazma/gözlem uçları) ---
builder.Services
    .AddAuthentication(ApiKeyPolicy.Name)
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyPolicy.Name, _ => { });

builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy(ApiKeyPolicy.Name, policy => policy
        .AddAuthenticationSchemes(ApiKeyPolicy.Name)
        .RequireAuthenticatedUser());

// --- Gelen istek limiti (açık demo kötüye kullanıma karşı, S6) ---
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));
});

// --- Hata sözleşmesi (RFC 7807) ---
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// --- OpenAPI ---
builder.Services.AddOpenApi();

// --- CORS (dashboard tarayıcı çağrıları) ---
var corsOrigins = builder.Configuration.GetSection("ContentHub:Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOrigins.Length == 0)
        {
            // Demo varsayılanı: açık. Üretimde ContentHub:Cors:AllowedOrigins ile kısıtlayın.
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
        else
        {
            policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod();
        }
    });
});

var app = builder.Build();

// Yerel/demo kolaylığı: şemayı modelden kur (üretimde `dotnet ef database update`).
if (app.Configuration.GetValue<bool>("ContentHub:InitializeDatabase"))
{
    await app.Services.InitializeDatabaseAsync();
}

app.UseExceptionHandler();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// --- Doküman ---
app.MapOpenApi();
app.MapScalarApiReference(); // /scalar

// --- Sağlık/uyandırma (Kanvas "uykudaki API") ---
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timeUtc = DateTimeOffset.UtcNow }))
    .WithTags("System")
    .WithName("HealthCheck");

// --- Modül uçları ---
foreach (var module in modules)
{
    module.MapEndpoints(app);
}

app.Run();

// Entegrasyon testleri (WebApplicationFactory<Program>) için.
public partial class Program;
