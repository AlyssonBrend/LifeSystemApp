using System.Text;
using Anthropic;
using Anthropic.Models.Messages;

namespace LifeSystem.Api.Services;

/// <summary>
/// Porta para a IA Mentora (PRD 4.5). Interface fina para os testes usarem um fake
/// sem tocar a rede — o jogo nunca depende da IA para funcionar (Nível 1 cobre tudo).
/// </summary>
public interface IClienteIa
{
    bool Configurado { get; }
    Task<string> Gerar(string sistema, string contexto);
}

/// <summary>
/// Cliente da API Claude via SDK oficial. A chave vem de Ia:Chave (user-secrets no dev,
/// Ia__Chave em produção) ou da variável ANTHROPIC_API_KEY. Sem chave, o módulo Mentor
/// fica desabilitado com aviso amigável — o resto do jogo segue normal.
/// </summary>
public class ClienteIaAnthropic(IConfiguration config) : IClienteIa
{
    private readonly string? _chave =
        config["Ia:Chave"] ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
    private readonly string _modelo = config["Ia:Modelo"] ?? "claude-opus-4-8";

    public bool Configurado => !string.IsNullOrWhiteSpace(_chave);

    public async Task<string> Gerar(string sistema, string contexto)
    {
        if (!Configurado)
            throw new InvalidOperationException(
                "IA Mentora não configurada — defina a chave com 'dotnet user-secrets set Ia:Chave <sua-chave>' na pasta da API");

        AnthropicClient cliente = new() { ApiKey = _chave };
        var resposta = await cliente.Messages.Create(new MessageCreateParams
        {
            Model = _modelo,
            MaxTokens = 2000,
            Thinking = new ThinkingConfigAdaptive(),
            System = sistema,
            Messages = [new() { Role = Role.User, Content = contexto }],
        });

        var texto = new StringBuilder();
        foreach (var bloco in resposta.Content.Select(b => b.Value).OfType<TextBlock>())
            texto.Append(bloco.Text);
        return texto.ToString().Trim();
    }
}
