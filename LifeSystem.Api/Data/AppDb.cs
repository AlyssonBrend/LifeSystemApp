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
    }
}
