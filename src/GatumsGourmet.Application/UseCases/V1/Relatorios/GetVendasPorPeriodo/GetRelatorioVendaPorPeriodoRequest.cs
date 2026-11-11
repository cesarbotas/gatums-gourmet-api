namespace GatumsGourmet.Application.UseCases.V1.Relatorios.GetVendasPorPeriodo;

public class GetRelatorioVendaPorPeriodoRequest
{
    public DateTime DataInicio { get; private set; } = default!;
    public DateTime DataFim { get; private set; } = default!;

    protected GetRelatorioVendaPorPeriodoRequest() { }

    public static GetRelatorioVendaPorPeriodoRequest Criar(
        DateTime dataInicio,
        DateTime dataFim)
    {
        return new GetRelatorioVendaPorPeriodoRequest
        {
            DataInicio = dataInicio,
            DataFim = dataFim
        };
    }
}
