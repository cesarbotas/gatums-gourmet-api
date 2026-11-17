namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPix;

public record IniciarPixRequest(
    Guid PedidoId,
    decimal? Valor = null,
    string? PayerEmail = null,
    string? PayerNome = null
);
