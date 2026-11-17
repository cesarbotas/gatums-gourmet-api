namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarPagamentoCartao;

public record ProcessarPagamentoCartaoRequest(
    string Numero,
    string Nome,
    string Validade,
    string Cvv
);