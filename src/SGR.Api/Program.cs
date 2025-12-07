using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SGR.Api.Data;
using SGR.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configurar serialização JSON para usar camelCase (padrão do Angular)
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = false;
    });
builder.Services.AddOpenApi();

// Configure Entity Framework Core
builder.Services.AddApplicationDbContext(builder.Configuration);

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada");

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
});

// Register Application Services
builder.Services.AddApplicationServices();

// Configure CORS
builder.Services.AddApplicationCors(builder.Configuration);

// Add Health Checks
builder.Services.AddApplicationHealthChecks(builder.Configuration);

var app = builder.Build();

// Apply Migrations and Initialize Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Aplicar migrations pendentes
        var pendingMigrations = context.Database.GetPendingMigrations().ToList();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("Aplicando {Count} migration(s) pendente(s)...", pendingMigrations.Count);
            context.Database.Migrate();
            logger.LogInformation("Migrations aplicadas com sucesso.");
        }
        
        // Inicializar dados padrão (após migrations)
        DbInitializer.Initialize(context);
        logger.LogInformation("Banco de dados inicializado com sucesso.");
        
        // Migrar schemas de tenant existentes (corrigir estruturas antigas)
        try
        {
            var tenantService = services.GetRequiredService<SGR.Api.Services.Interfaces.ITenantService>();
            await tenantService.MigrateAllTenantSchemasAsync();
            logger.LogInformation("Migração de schemas de tenant concluída com sucesso.");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Erro ao migrar schemas de tenant (pode ser normal se não houver tenants existentes).");
            // Não falha a inicialização se a migração de tenants falhar
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Ocorreu um erro ao inicializar o banco de dados.");
        // Em produção, você pode querer falhar a inicialização
        // throw;
    }
}

// Configure the HTTP request pipeline.
// IMPORTANTE: UseCors() deve vir ANTES de UseHttpsRedirection() para evitar problemas com preflight
app.UseMiddleware<SGR.Api.Middleware.ExceptionHandlingMiddleware>();
app.UseCors();

// Middleware de identificação do tenant (deve vir antes de UseAuthentication)
app.UseMiddleware<SGR.Api.Middleware.TenantIdentificationMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Em desenvolvimento, nÃ£o forÃ§ar HTTPS redirection para evitar problemas com CORS
}
else
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();

// Health Checks
app.MapHealthChecks("/health");

app.MapControllers();

app.Run();


