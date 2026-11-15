using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Application.UseCases.V1.Relatorios.GetVendasPorPeriodo;

namespace GatumsGourmet.Application.UseCases.V1.Relatorios.GetVendasPorPeriodo;

public class GetVendasPorPeriodoHandler : IGetVendasPorPeriodoHandler
{
    private readonly IVendaRepository _vendaRepository;

    public GetVendasPorPeriodoHandler(IVendaRepository vendaRepository)
    {
        _vendaRepository = vendaRepository;
    }

    public async Task<Result<VendasPorPeriodoResponse>> Handle(GetRelatorioVendaPorPeriodoRequest request,
        CancellationToken ct)
    {
        var response = new VendasPorPeriodoResponse();

        var vendas = await _vendaRepository
            .ConsultaPorPeriodo(request.DataInicio, request.DataFim, ct);

        if (vendas == null || !vendas.Any())
        {
            return Result<VendasPorPeriodoResponse>.Ok(response);
        }

        response.Vendas = vendas.Select(v => new VendaPorPeriodoItemResponse
        {
            Metodo = string.Empty,
            Quantidade = 0,
            Total = v.Total,
            Aprovados = 0,
            Recusados = 0,
            Expirados = 0
        }).ToList();

        return Result<VendasPorPeriodoResponse>.Ok(response);
    }
}
