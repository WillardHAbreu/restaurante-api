using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Restaurante.Api.Dados;
using Restaurante.Api.Modelos;
using Restaurante.Api.Servicos;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection String: vem de variável de ambiente (Render/Neon) ou do appsettings.Development.json (local).
// "Data Source=..." usa SQLite (só para testar na sua máquina); qualquer outra coisa usa Postgres.
var connectionString = builder.Configuration.GetConnectionString("Padrao")
    ?? Environment.GetEnvironmentVariable("ConnectionStrings__Padrao")
    ?? throw new InvalidOperationException("Connection string 'Padrao' não encontrada.");

var usaSqlite = connectionString.TrimStart().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (usaSqlite) options.UseSqlite(connectionString);
    else options.UseNpgsql(connectionString);
});

// 2. Serviços e Injeção de Dependência
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<RestauranteServico>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// 3. Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// 4. Autenticação JWT
var jwtSegredo = builder.Configuration["Jwt:Segredo"] 
    ?? Environment.GetEnvironmentVariable("Jwt__Segredo") 
    ?? "chave-secreta-padrao-para-desenvolvimento-com-mais-de-32-caracteres";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSegredo)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

// 5. Configuração do Swashbuckle / Swagger (LIGADO também em produção)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "API Restaurante com Reserva de Mesas",
        Version = "v1",
        Description = "Projeto escolar empresa fictícia - API REST de reservas"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. Exemplo: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    // Fato técnico 4: Padrão Swashbuckle 10 / OpenAPI
    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc),
            new List<string>()
        }
    });
});

// 6. CORS
var corsOrigens = builder.Configuration["Cors:Origens"] ?? Environment.GetEnvironmentVariable("Cors_Origens") ?? "http://localhost:5173";
var origensArray = corsOrigens.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(origensArray)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Location");
    });
});

var app = builder.Build();

// 7. Middleware de Erros e ProblemDetails
app.UseExceptionHandler();
app.UseStatusCodePages();

// 8. Swagger ativado globalmente (inclusive em produção)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Restaurante API v1");
    c.RoutePrefix = string.Empty; // Abre o Swagger na raiz da API (redireciona / para o swagger)
});

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// Rota pública de Versão
app.MapGet("/api/versao", () => new
{
    commit = Environment.GetEnvironmentVariable("RENDER_GIT_COMMIT") ?? "local",
    ambiente = app.Environment.EnvironmentName,
    dataHoraUtc = DateTime.UtcNow
});

// 9. Executar Migrations e Seed Idempotente na subida
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Postgres: aplica as migrations (histórico versionado). SQLite local: cria o banco direto do modelo,
    // porque as migrations são geradas para Postgres.
    if (usaSqlite) await db.Database.EnsureCreatedAsync();
    else await db.Database.MigrateAsync();

    // Seed Idempotente
    var adminSenha = builder.Configuration["Seed:AdminSenha"] 
        ?? Environment.GetEnvironmentVariable("Seed__AdminSenha") 
        ?? "Admin@2026";

    if (!await db.Usuarios.AnyAsync(u => u.Email == "admin@exemplo.com"))
    {
        db.Usuarios.Add(new Usuario
        {
            Nome = "Administrador",
            Email = "admin@exemplo.com",
            SenhaHash = BCrypt.Net.BCrypt.HashPassword(adminSenha),
            Papel = "admin"
        });
    }

    if (!await db.Usuarios.AnyAsync(u => u.Email == "cliente1@exemplo.com"))
    {
        db.Usuarios.Add(new Usuario
        {
            Nome = "Cliente Um",
            Email = "cliente1@exemplo.com",
            SenhaHash = BCrypt.Net.BCrypt.HashPassword("Cliente@123"),
            Papel = "cliente"
        });
    }

    if (!await db.Usuarios.AnyAsync(u => u.Email == "cliente2@exemplo.com"))
    {
        db.Usuarios.Add(new Usuario
        {
            Nome = "Cliente Dois",
            Email = "cliente2@exemplo.com",
            SenhaHash = BCrypt.Net.BCrypt.HashPassword("Cliente@123"),
            Papel = "cliente"
        });
    }

    // Seed de Mesas (8+ registros realistas)
    if (!await db.Mesas.AnyAsync())
    {
        db.Mesas.AddRange(
            new Mesa { NumeroOuNome = "Mesa 01", CapacidadePessoas = 2, Localizacao = "Salão Principal - Janela" },
            new Mesa { NumeroOuNome = "Mesa 02", CapacidadePessoas = 4, Localizacao = "Salão Principal - Centro" },
            new Mesa { NumeroOuNome = "Mesa 03", CapacidadePessoas = 4, Localizacao = "Salão Principal - Centro" },
            new Mesa { NumeroOuNome = "Mesa 04", CapacidadePessoas = 6, Localizacao = "Salão Principal - Fundo" },
            new Mesa { NumeroOuNome = "Varanda 01", CapacidadePessoas = 2, Localizacao = "Área Externa Coberta" },
            new Mesa { NumeroOuNome = "Varanda 02", CapacidadePessoas = 4, Localizacao = "Área Externa Coberta" },
            new Mesa { NumeroOuNome = "VIP 01", CapacidadePessoas = 8, Localizacao = "Área Privativa Superior" },
            new Mesa { NumeroOuNome = "VIP 02", CapacidadePessoas = 10, Localizacao = "Área Privativa Superior" }
        );
    }

    await db.SaveChangesAsync();
}

app.Run();

// Necessário para os testes de serviço (SQLite em memória, só no projeto de testes)
public partial class Program { }