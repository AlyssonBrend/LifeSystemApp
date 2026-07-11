using System.Security.Claims;
using System.Text;
using LifeSystem.Api.Contracts;
using LifeSystem.Api.Data;
using LifeSystem.Api.Domain;
using LifeSystem.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// SQLite no dev; PostgreSQL em produção (PRD seção 6). Railway/Render fornecem DATABASE_URL.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
builder.Services.AddDbContext<AppDb>(opt =>
{
    if (!string.IsNullOrEmpty(databaseUrl))
        opt.UseNpgsql(ConverterDatabaseUrl(databaseUrl));
    else
        opt.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=lifesystem.db");
});

// postgres://usuario:senha@host:porta/banco  →  formato Npgsql
static string ConverterDatabaseUrl(string url)
{
    var uri = new Uri(url);
    var partes = uri.UserInfo.Split(':', 2);
    return $"Host={uri.Host};Port={(uri.Port > 0 ? uri.Port : 5432)};Database={uri.AbsolutePath.TrimStart('/')};" +
           $"Username={Uri.UnescapeDataString(partes[0])};Password={Uri.UnescapeDataString(partes[1])};SSL Mode=Require;Trust Server Certificate=true";
}

builder.Services.AddScoped<JogoService>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<IRelogio, RelogioJogo>();

// Sem chave não sobe: evita rodar produção com a chave de exemplo
var jwtChave = builder.Configuration["Jwt:Chave"];
if (string.IsNullOrWhiteSpace(jwtChave))
    throw new InvalidOperationException(
        "Jwt:Chave não configurada. Dev: rode 'dotnet user-secrets set Jwt:Chave <valor-aleatorio>' na pasta da API. " +
        "Produção: defina a variável de ambiente Jwt__Chave.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Emissor"],
            ValidAudience = builder.Configuration["Jwt:Audiencia"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtChave)),
        };
    });
builder.Services.AddAuthorization();

// Produção: restrinja às origens reais em Cors:Origens (ex.: https://lifesystem.vercel.app).
// Sem a configuração (dev/rede local), libera qualquer origem — auth vai no header Bearer.
var origensPermitidas = builder.Configuration.GetSection("Cors:Origens").Get<string[]>();
builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
{
    if (origensPermitidas is { Length: > 0 })
        p.WithOrigins(origensPermitidas).AllowAnyHeader().AllowAnyMethod();
    else
        p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
}));

// Railway/Render informam a porta via PORT
if (Environment.GetEnvironmentVariable("PORT") is { } porta)
    builder.WebHost.UseUrls($"http://0.0.0.0:{porta}");

var app = builder.Build();

using (var escopo = app.Services.CreateScope())
    escopo.ServiceProvider.GetRequiredService<AppDb>().Database.Migrate();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ---------- Auth ----------

app.MapPost("/api/auth/registrar", async (RegistrarReq req, AppDb db, TokenService tokens) =>
{
    var email = req.Email.Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        return Results.BadRequest(new { erro = "E-mail inválido" });
    if (req.Senha.Length < 6)
        return Results.BadRequest(new { erro = "A senha precisa de pelo menos 6 caracteres" });
    if (string.IsNullOrWhiteSpace(req.NomePersonagem))
        return Results.BadRequest(new { erro = "Dê um nome ao seu personagem" });
    if (await db.Usuarios.AnyAsync(u => u.Email == email))
        return Results.Conflict(new { erro = "Este e-mail já tem uma conta" });

    var usuario = new Usuario { Email = email, SenhaHash = SenhaHasher.Gerar(req.Senha) };
    db.Usuarios.Add(usuario);
    await db.SaveChangesAsync();

    usuario.Personagem = JogoService.NovoPersonagem(usuario.Id, req.NomePersonagem.Trim());
    db.Personagens.Add(usuario.Personagem);
    await db.SaveChangesAsync();

    return Results.Ok(new AuthResp(tokens.Gerar(usuario.Id, email), email, usuario.Personagem.Nome));
});

app.MapPost("/api/auth/login", async (LoginReq req, AppDb db, TokenService tokens) =>
{
    var email = req.Email.Trim().ToLowerInvariant();
    var usuario = await db.Usuarios.Include(u => u.Personagem)
        .FirstOrDefaultAsync(u => u.Email == email);
    if (usuario is null || !SenhaHasher.Verificar(req.Senha, usuario.SenhaHash))
        return Results.Unauthorized();
    return Results.Ok(new AuthResp(tokens.Gerar(usuario.Id, email), email, usuario.Personagem.Nome));
});

// ---------- Jogo ----------

static int UsuarioId(ClaimsPrincipal user) =>
    int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? user.FindFirstValue("sub")!);

static async Task<IResult> ComAcao(
    ClaimsPrincipal user, JogoService jogo, AppDb db,
    Func<Personagem, Task<List<EventoDto>>>? acao = null)
{
    var p = await jogo.CarregarPersonagem(UsuarioId(user));
    var eventos = await jogo.Sincronizar(p);
    try
    {
        if (acao is not null) eventos.AddRange(await acao(p));
    }
    catch (Exception e) when (e is ArgumentException or InvalidOperationException)
    {
        await db.SaveChangesAsync(); // persiste a sincronização mesmo com ação inválida
        return Results.BadRequest(new { erro = e.Message });
    }
    await db.SaveChangesAsync();
    return Results.Ok(new AcaoResp(await jogo.MontarEstado(p), eventos));
}

var api = app.MapGroup("/api/jogo").RequireAuthorization();

api.MapGet("/estado", (ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComAcao(u, j, db));

api.MapPost("/missoes/{id}/concluir", (string id, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComAcao(u, j, db, p => j.ConcluirMissao(p, id)));

api.MapPost("/missoes/{id}/checklist", (string id, CheckReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComAcao(u, j, db, p => j.MarcarCheck(p, id, req.Indice, req.Marcado)));

api.MapPost("/foco/iniciar", (FocoIniciarReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComAcao(u, j, db, p => j.IniciarFoco(p, req.Tipo)));

api.MapPost("/foco/encerrar", (FocoEncerrarReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComAcao(u, j, db, p => j.EncerrarFoco(p, req.Abandonar)));

api.MapPost("/classe", (ClasseReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComAcao(u, j, db, p => j.EscolherClasse(p, req.Classe)));

api.MapPost("/chefe/recompensa", (RecompensaReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComAcao(u, j, db, p => j.DefinirRecompensaCaixa(p, req.Texto)));

api.MapPut("/economias", (EconomiasReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComAcao(u, j, db, p => j.DefinirEconomias(p, req.Valor)));

api.MapPost("/loja/itens", (ItemLojaReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComAcao(u, j, db, p => j.AdicionarItemLoja(p, req.Nome, req.Preco)));

api.MapPost("/loja/itens/{itemId:int}/comprar", (int itemId, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComAcao(u, j, db, p => j.ComprarItem(p, itemId)));

app.MapGet("/api/saude", () => Results.Ok(new { ok = true, agora = DateTime.UtcNow }));

app.Run();
