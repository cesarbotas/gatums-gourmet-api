using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Infrastructure.Services.Pagamentos;

// Mock implementation for Development â€” replace with Stripe/PagSeguro in Production
public class MockPaymentGateway : IPaymentGateway
{
    public Task<CartaoResult> ProcessarAsync(Guid transacaoId, decimal valor, MetodoPagamento metodo, CartaoInput cartao, CancellationToken ct = default)
    {
        var ultimos4 = cartao.Numero.Length >= 4 ? cartao.Numero[^4..] : "0000";
        var result = new CartaoResult(true, $"mock-ref-{transacaoId}", ultimos4, "VISA", null);
        return Task.FromResult(result);
    }
}
