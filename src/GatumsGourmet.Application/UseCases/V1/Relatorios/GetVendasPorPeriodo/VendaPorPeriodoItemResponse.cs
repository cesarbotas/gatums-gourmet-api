namespace GatumsGourmet.Application.UseCases.V1.Relatorios.GetVendasPorPeriodo;

public class VendaPorPeriodoItemResponse
{
    public string Metodo { get; set; }
    public int Quantidade { get; set; }
    public decimal Total { get; set; }
    public int Aprovados { get; set; }
    public int Recusados { get; set; }
    public int Expirados { get; set; }
}