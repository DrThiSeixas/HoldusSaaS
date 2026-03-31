using Holdus.API.Extensions;
using Holdus.API.Middleware;
using Holdus.Application.Services;
using Holdus.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════
// SERVICES
// ═══════════════════════════════════════════════════════════════

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddCorsPolicy();
builder.Services.AddApiRateLimiting(builder.Configuration);
builder.Services.AddHoldusServices();

// Redis (opcional em dev — se não tiver, usa cache em memória)
var redisConn = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConn) && redisConn != "localhost:6379,abortConnect=false")
{
    builder.Services.AddRedisCache(builder.Configuration);
}
else
{
    builder.Services.AddDistributedMemoryCache(); // Fallback sem Redis
}

// Application services
builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy =
            System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Holdus Tríade API",
        Version = "v1",
        Description = "API do sistema de constituição de holdings familiares — Método Tríade Capital®"
    });

    // JWT no Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Token JWT. Exemplo: eyJhbGciOiJIUzI1NiIs..."
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

// Health checks (só PostgreSQL por enquanto)
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!, name: "postgresql");

// ═══════════════════════════════════════════════════════════════
// APP PIPELINE
// ═══════════════════════════════════════════════════════════════

var app = builder.Build();

// Auto-create database (Railway cria as tabelas no primeiro boot)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    try
    {
        await db.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(app.Services);
        Console.WriteLine("✓ Banco de dados OK.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Erro ao conectar banco: {ex.Message}");
    }
}

// Swagger sempre ativo (em produção usa /swagger)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Holdus API v1");
    c.RoutePrefix = "swagger";
});

// HTTPS redirect apenas em dev local (Railway gerencia SSL no proxy)
if (app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseTenantMiddleware();  // Após auth, antes dos controllers

app.MapControllers();
app.MapHealthChecks("/health");

// Rota raiz
app.MapGet("/", () => Results.Ok(new
{
    app = "Holdus Tríade SaaS",
    version = "2.0.0",
    status = "running",
    docs = "/swagger"
}));

app.Run();
