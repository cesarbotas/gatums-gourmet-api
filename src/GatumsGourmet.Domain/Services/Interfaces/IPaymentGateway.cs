using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Enums.Pagamentos;

namespace GatumsGourmet.Domain.Services.Interfaces;

public interface IPaymentGateway
{
    Task<CartaoResult> ProcessarAsync(Guid transacaoId, decimal valor, MetodoPagamento metodo, CartaoInput cartao, CancellationToken ct = default);
}
