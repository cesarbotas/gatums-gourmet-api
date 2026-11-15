using System.Collections.Generic;

namespace GatumsGourmet.Application.UseCases.V1.Relatorios.GetVendasPorPeriodo;

public class VendasPorPeriodoResponse
{
    public IEnumerable<VendaPorPeriodoItemResponse> Vendas { get; set; }

    public VendasPorPeriodoResponse()
    {
        Vendas = Enumerable.Empty<VendaPorPeriodoItemResponse>();
    }
}
