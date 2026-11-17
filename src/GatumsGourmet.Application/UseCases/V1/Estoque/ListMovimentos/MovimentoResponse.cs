namespace GatumsGourmet.Application.UseCases.V1.Estoque.ListMovimentos;

public record MovimentoResponse(
    Guid Id,
    string Tipo,
    int Quantidade,
    int SaldoResultante,
    Guid? PedidoId,
    string? Observacao,
    DateTime Data
);