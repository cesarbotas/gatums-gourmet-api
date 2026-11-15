using GatumsGourmet.Domain.Entities.Pagamentos;

namespace GatumsGourmet.Domain.Services.Interfaces;

public interface IPixProvider
{
    Task<PixResult> GerarAsync(Guid transacaoId, decimal valor, CancellationToken ct = default);

    Task<PixResult> GerarAsync(Guid transacaoId, decimal valor, string? descricao, string? payerEmail, string? payerNome, CancellationToken ct = default) =>
        GerarAsync(transacaoId, valor, ct);

    Task<bool> ConfirmarAsync(Guid transacaoId, CancellationToken ct = default);

    Task<string?> ConsultarStatusAsync(string operadoraRef, CancellationToken ct = default) =>
        Task.FromResult<string?>("approved");
}
