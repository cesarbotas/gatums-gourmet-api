using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Enums.Pagamentos;

namespace GatumsGourmet.Application.UseCases.V2.Pagamentos;

public static class PixOrderStatusMapper
{
    /// <summary>Mapeia o status da order do Mercado Pago; <c>null</c> = continua pendente.</summary>
    public static StatusTransacao? ToStatusTransacao(PixOrderStatus s)
    {
        if (string.Equals(s.Status, "processed", StringComparison.OrdinalIgnoreCase)
            && string.Equals(s.StatusDetail, "accredited", StringComparison.OrdinalIgnoreCase))
            return StatusTransacao.Aprovado;

        if (string.Equals(s.Status, "canceled", StringComparison.OrdinalIgnoreCase)
            || string.Equals(s.Status, "failed", StringComparison.OrdinalIgnoreCase))
            return StatusTransacao.Recusado;

        return null;
    }
}