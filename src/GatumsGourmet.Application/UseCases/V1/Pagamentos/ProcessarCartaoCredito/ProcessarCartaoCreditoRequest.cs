namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarCartaoCredito;

public record ProcessarCartaoCreditoRequest(
    Guid PedidoId,
    decimal? Valor,
    string Numero,
    string Nome,
    string Validade,
    string Cvv,
    int Parcelas = 1
);