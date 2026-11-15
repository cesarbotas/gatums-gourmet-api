namespace GatumsGourmet.Domain.Entities.Pagamentos;

public record PixOrderRequest(
    Guid TransacaoId,
    decimal Valor,
    string? Descricao,
    string PayerEmail,
    string? PayerNome);