using LifeSystem.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace LifeSystem.Api.Data;

public class AppDb(DbContextOptions<AppDb> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Personagem> Personagens => Set<Personagem>();
    public DbSet<MissaoLog> MissoesLog => Set<MissaoLog>();
    public DbSet<ChefeInstancia> ChefesInstancias => Set<ChefeInstancia>();
    public DbSet<ConquistaDesbloqueada> ConquistasDesbloqueadas => Set<ConquistaDesbloqueada>();
    public DbSet<ItemLoja> ItensLoja => Set<ItemLoja>();
    public DbSet<TransacaoMoedas> TransacoesMoedas => Set<TransacaoMoedas>();
    public DbSet<SessaoFoco> SessoesFoco => Set<SessaoFoco>();
    public DbSet<PerfilCorporal> PerfisCorporais => Set<PerfilCorporal>();
    public DbSet<RegistroCarga> RegistrosCarga => Set<RegistroCarga>();
    public DbSet<RegistroCardio> RegistrosCardio => Set<RegistroCardio>();
    public DbSet<Amizade> Amizades => Set<Amizade>();
    public DbSet<PerfilFinanceiro> PerfisFinanceiros => Set<PerfilFinanceiro>();
    public DbSet<Divida> Dividas => Set<Divida>();
    public DbSet<AporteEconomia> Aportes => Set<AporteEconomia>();
    public DbSet<Habilidade> Habilidades => Set<Habilidade>();
    public DbSet<MarcoConcluido> MarcosConcluidos => Set<MarcoConcluido>();
    public DbSet<Livro> Livros => Set<Livro>();
    public DbSet<InteracaoSocial> InteracoesSociais => Set<InteracaoSocial>();
    public DbSet<ConselhoMentor> ConselhosMentor => Set<ConselhoMentor>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
        mb.Entity<Usuario>()
            .HasOne(u => u.Personagem)
            .WithOne()
            .HasForeignKey<Personagem>(p => p.UsuarioId);

        mb.Entity<MissaoLog>().HasIndex(m => new { m.PersonagemId, m.MissaoId, m.Data }).IsUnique();
        mb.Entity<ChefeInstancia>().HasIndex(c => new { c.PersonagemId, c.SemanaInicio }).IsUnique();
        mb.Entity<ConquistaDesbloqueada>().HasIndex(c => new { c.PersonagemId, c.ConquistaId }).IsUnique();

        mb.Entity<Personagem>()
            .HasMany(p => p.Missoes).WithOne().HasForeignKey(m => m.PersonagemId);
        mb.Entity<Personagem>()
            .HasMany(p => p.Chefes).WithOne().HasForeignKey(c => c.PersonagemId);
        mb.Entity<Personagem>()
            .HasMany(p => p.Conquistas).WithOne().HasForeignKey(c => c.PersonagemId);
        mb.Entity<Personagem>()
            .HasMany(p => p.Loja).WithOne().HasForeignKey(i => i.PersonagemId);
        mb.Entity<Personagem>()
            .HasMany(p => p.Transacoes).WithOne().HasForeignKey(t => t.PersonagemId);
        mb.Entity<Personagem>()
            .HasMany(p => p.SessoesFoco).WithOne().HasForeignKey(s => s.PersonagemId);

        // Fase 2 — Corpo
        mb.Entity<Personagem>().HasIndex(p => p.CodigoAmigo).IsUnique();
        mb.Entity<Personagem>()
            .HasOne(p => p.PerfilCorporal).WithOne().HasForeignKey<PerfilCorporal>(pc => pc.PersonagemId);
        mb.Entity<RegistroCarga>().HasIndex(r => new { r.PersonagemId, r.ExercicioId, r.Data });
        mb.Entity<RegistroCardio>().HasIndex(r => new { r.PersonagemId, r.Data });
        mb.Entity<Amizade>().HasIndex(a => new { a.SolicitanteId, a.ConvidadoId }).IsUnique();

        // Fase 3 — Mente e Bolso
        mb.Entity<Personagem>()
            .HasOne(p => p.PerfilFinanceiro).WithOne().HasForeignKey<PerfilFinanceiro>(pf => pf.PersonagemId);
        mb.Entity<Divida>().HasIndex(d => d.PersonagemId);
        mb.Entity<AporteEconomia>().HasIndex(a => new { a.PersonagemId, a.Data });
        mb.Entity<Habilidade>().HasIndex(h => h.PersonagemId);
        mb.Entity<Habilidade>()
            .HasMany(h => h.Marcos).WithOne().HasForeignKey(m => m.HabilidadeId);
        mb.Entity<MarcoConcluido>().HasIndex(m => new { m.HabilidadeId, m.MarcoIndice }).IsUnique();
        mb.Entity<Livro>().HasIndex(l => l.PersonagemId);
        mb.Entity<InteracaoSocial>().HasIndex(i => new { i.PersonagemId, i.Data }).IsUnique();

        // Fase 4 — IA Mentora
        mb.Entity<ConselhoMentor>().HasIndex(c => new { c.PersonagemId, c.Data });
    }
}
