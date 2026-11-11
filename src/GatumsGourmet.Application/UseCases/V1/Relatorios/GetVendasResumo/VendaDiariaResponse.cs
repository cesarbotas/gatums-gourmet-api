namespace GatumsGourmet.Application.UseCases.V1.Relatorios.GetVendasResumo;

public record VendaDiariaResponse(
    string Data,
    decimal Faturamento,
    int Pedidos
);
