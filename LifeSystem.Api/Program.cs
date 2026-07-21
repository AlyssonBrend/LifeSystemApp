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
builder.Services.AddSingleton<IClienteIa, ClienteIaAnthropic>(); // Fase 4 — IA Mentora (PRD 4.5)

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
    ComAcao(u, j, db, p => j.IniciarFoco(p, req.Tipo, req.HabilidadeId)));

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

// ---------- Fase 2 — Corpo (PRD 4.2 e 4.3) ----------
// Mesmo fluxo do ComAcao, mas a resposta inclui o estado do módulo Corpo (a aba atualiza numa ida só)

static async Task<IResult> ComCorpo(
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
        await db.SaveChangesAsync();
        return Results.BadRequest(new { erro = e.Message });
    }
    await db.SaveChangesAsync();
    var corpo = await jogo.MontarCorpo(p); // pode gerar o código de amigo no primeiro acesso
    await db.SaveChangesAsync();
    return Results.Ok(new AcaoResp(await jogo.MontarEstado(p), eventos, corpo));
}

var corpo = api.MapGroup("/corpo");

corpo.MapGet("", (ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComCorpo(u, j, db));

corpo.MapPut("/perfil", (PerfilReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComCorpo(u, j, db, p => j.DefinirPerfilCorporal(p, req)));

corpo.MapPost("/aviso", (ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComCorpo(u, j, db, p => j.AceitarAvisoSaude(p)));

corpo.MapPost("/carga", (CargaReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComCorpo(u, j, db, p => j.RegistrarCarga(p, req.ExercicioId, req.CargaKg, req.Reps)));

corpo.MapPost("/cardio", (CardioReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComCorpo(u, j, db, p => j.RegistrarCardio(p, req.DistanciaKm, req.DuracaoMin)));

corpo.MapPut("/ranking", (RankingOptInReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComCorpo(u, j, db, p => j.DefinirRankingOptIn(p, req.OptIn)));

api.MapPost("/amigos", (AmigoReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComCorpo(u, j, db, p => j.AdicionarAmigo(p, req.Codigo)));

api.MapPost("/amigos/{amizadeId:int}/responder", (int amizadeId, ResponderAmizadeReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComCorpo(u, j, db, p => j.ResponderAmizade(p, amizadeId, req.Aceitar)));

// ---------- Fase 3 — Mente e Bolso (PRD 4.1 e 4.4) ----------
// Mesmo fluxo do ComCorpo: a resposta inclui o estado do módulo (a aba atualiza numa ida só)

static async Task<IResult> ComFinancas(
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
        await db.SaveChangesAsync();
        return Results.BadRequest(new { erro = e.Message });
    }
    await db.SaveChangesAsync();
    return Results.Ok(new AcaoResp(await jogo.MontarEstado(p), eventos, Financas: await jogo.MontarFinancas(p)));
}

static async Task<IResult> ComMente(
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
        await db.SaveChangesAsync();
        return Results.BadRequest(new { erro = e.Message });
    }
    await db.SaveChangesAsync();
    return Results.Ok(new AcaoResp(await jogo.MontarEstado(p), eventos, Mente: await jogo.MontarMente(p)));
}

var financas = api.MapGroup("/financas");

financas.MapGet("", (ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComFinancas(u, j, db));

financas.MapPut("/perfil", (PerfilFinanceiroReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComFinancas(u, j, db, p => j.DefinirPerfilFinanceiro(p, req)));

financas.MapPost("/aviso", (ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComFinancas(u, j, db, p => j.AceitarAvisoFinancas(p)));

financas.MapPost("/aporte", (AporteReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComFinancas(u, j, db, p => j.RegistrarAporte(p, req.Valor)));

financas.MapPost("/dividas", (DividaReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComFinancas(u, j, db, p => j.CriarDivida(p, req.Nome, req.Valor, req.JurosPctMes)));

financas.MapPost("/dividas/{dividaId:int}/pagar", (int dividaId, PagarDividaReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComFinancas(u, j, db, p => j.PagarDivida(p, dividaId, req.Valor)));

financas.MapPost("/converter", (ConverterReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComFinancas(u, j, db, p => j.ConverterMoedas(p, req.Moedas)));

var mente = api.MapGroup("/mente");

mente.MapGet("", (ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComMente(u, j, db));

mente.MapPost("/habilidades", (HabilidadeReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComMente(u, j, db, p => j.CriarHabilidade(p, req.TrilhaId, req.Nome)));

mente.MapPost("/habilidades/{habilidadeId:int}/marcos/{indice:int}", (int habilidadeId, int indice, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComMente(u, j, db, p => j.ConcluirMarco(p, habilidadeId, indice)));

mente.MapPost("/livros", (LivroReq req, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComMente(u, j, db, p => j.CriarLivro(p, req.Titulo, req.HabilidadeId)));

mente.MapPost("/livros/{livroId:int}/concluir", (int livroId, ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComMente(u, j, db, p => j.ConcluirLivro(p, livroId)));

mente.MapPost("/social", (ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComMente(u, j, db, p => j.RegistrarInteracaoSocial(p)));

// ---------- Fase 4 — IA Mentora (PRD 4.5) ----------

static async Task<IResult> ComMentor(
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
        await db.SaveChangesAsync();
        return Results.BadRequest(new { erro = e.Message });
    }
    await db.SaveChangesAsync();
    return Results.Ok(new AcaoResp(await jogo.MontarEstado(p), eventos, Mentor: await jogo.MontarMentor(p)));
}

var mentor = api.MapGroup("/mentor");

mentor.MapGet("", (ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComMentor(u, j, db));

mentor.MapPost("/analisar", (ClaimsPrincipal u, JogoService j, AppDb db) =>
    ComMentor(u, j, db, p => j.AnalisarComMentor(p)));

app.MapGet("/api/saude", () => Results.Ok(new { ok = true, agora = DateTime.UtcNow }));

app.Run();
