namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarCartaoDebito;

public record ProcessarCartaoDebitoRequest(
    Guid PedidoId,
    decimal? Valor,
    string Numero,
    string Nome,
    string Validade,
    string Cvv
);
