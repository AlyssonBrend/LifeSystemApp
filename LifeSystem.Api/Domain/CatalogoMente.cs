namespace LifeSystem.Api.Domain;

// Catálogo da Fase 3 — Mente (PRD 4.4): árvore de conhecimento com trilhas e marcos.

public record TrilhaDef(string Id, string Nome, string Emoji, string[] Marcos);

public static class CatalogoMente
{
    // Trilhas com marcos escritos à mão (PRD 4.4). Habilidades personalizadas usam MarcosPorHoras.
    public static readonly TrilhaDef[] Trilhas =
    [
        new("csharp", "C#", "🎯",
        [
            "Sintaxe, tipos e coleções",
            "POO: classes, interfaces e herança",
            "LINQ e expressões lambda",
            "Async/await e tarefas",
            "Testes com xUnit",
            "Projeto completo publicado",
        ]),
        new("dotnet", ".NET", "🟣",
        [
            "Minimal APIs e injeção de dependência",
            "EF Core: migrations e consultas",
            "Autenticação com JWT",
            "Logs, configuração e ambientes",
            "Deploy em nuvem",
        ]),
        new("javascript", "JavaScript", "🟨",
        [
            "Fundamentos: escopo, arrays e objetos",
            "Funções, closures e o event loop",
            "Promises e async/await",
            "DOM e eventos",
            "Módulos e tooling moderno",
        ]),
        new("react", "React", "⚛️",
        [
            "Componentes e props",
            "Estado e efeitos (hooks)",
            "Formulários e listas",
            "Roteamento e dados remotos",
            "App completo no ar",
        ]),
        new("ingles", "Inglês", "🇬🇧",
        [
            "Rotina de estudo diária montada",
            "Conversa básica do dia a dia",
            "Entrevista de emprego simulada",
            "Filme/série sem legenda",
            "Reunião de trabalho conduzida em inglês",
        ]),
        new("financas", "Finanças pessoais", "💷",
        [
            "Orçamento mensal mapeado",
            "Regra 50/30/20 aplicada",
            "Reserva de emergência iniciada",
            "Primeiro investimento feito",
            "Plano de longo prazo escrito",
        ]),
    ];

    /// <summary>Marcos automáticos das habilidades personalizadas: destravam por horas de foco creditadas.</summary>
    public static readonly int[] MarcosPorHoras = [10, 25, 50, 100];
}
