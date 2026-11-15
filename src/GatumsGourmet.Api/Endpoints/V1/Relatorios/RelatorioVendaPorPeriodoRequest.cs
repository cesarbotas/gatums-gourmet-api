using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1.Relatorios;

public class RelatorioVendaPorPeriodoRequest
{
    [FromQuery(Name = "dataInicio")]
    public DateTime DataInicio { get; set; }
    [FromQuery(Name = "dataFim")]
    public DateTime DataFim { get; set; }
}