using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SGR.Api.Data;
using SGR.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
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

// Initialize Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Verificar se as colunas de auditoria já existem (migração temporária)
        // Em produção, isso deve ser feito via migration
        try
        {
            // Tentar adicionar colunas se não existirem (compatibilidade com banco existente)
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"Usuario\" ADD COLUMN IF NOT EXISTS \"UsuarioCriacao\" character varying(100);");
            context.Database.ExecuteSqlRaw(
                "ALTER TABLE \"Usuario\" ADD COLUMN IF NOT EXISTS \"DataCriacao\" timestamp with time zone NOT NULL DEFAULT (now() at time zone 'utc');");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Aviso ao verificar/adicionar colunas de auditoria. Isso é normal se as colunas já existem. Erro: {Message}", ex.Message);
        }

        DbInitializer.Initialize(context);
        logger.LogInformation("Banco de dados inicializado com sucesso.");
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


