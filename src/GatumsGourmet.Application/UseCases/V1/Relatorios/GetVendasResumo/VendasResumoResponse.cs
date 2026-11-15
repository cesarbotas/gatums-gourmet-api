namespace GatumsGourmet.Application.UseCases.V1.Relatorios.GetVendasResumo;

public record VendasResumoResponse(
    decimal Faturamento,
    int TotalPedidos,
    decimal TicketMedio,
    IReadOnlyList<TopProdutoResponse> TopProdutos,
    IReadOnlyList<VendaDiariaResponse> SerieDiaria
);
