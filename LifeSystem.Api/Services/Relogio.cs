namespace LifeSystem.Api.Services;

/// <summary>
/// O "hoje" do jogador segue o fuso configurado (Jogo:FusoHorario, padrão America/Sao_Paulo),
/// não o relógio do servidor — na nuvem a API roda em UTC e o dia viraria às 21h de Brasília.
/// Injetável para os testes controlarem datas.
/// </summary>
public interface IRelogio
{
    DateTime AgoraUtc { get; }
    DateOnly Hoje { get; }
    DateOnly DataDe(DateTime utc);
}

public class RelogioJogo(IConfiguration config) : IRelogio
{
    private readonly TimeZoneInfo _fuso = TimeZoneInfo.FindSystemTimeZoneById(
        config["Jogo:FusoHorario"] ?? "America/Sao_Paulo");

    public DateTime AgoraUtc => DateTime.UtcNow;
    public DateOnly Hoje => DataDe(DateTime.UtcNow);
    public DateOnly DataDe(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc), _fuso));
}
