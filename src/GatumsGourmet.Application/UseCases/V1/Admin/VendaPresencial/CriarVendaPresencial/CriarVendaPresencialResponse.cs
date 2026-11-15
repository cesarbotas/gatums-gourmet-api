namespace GatumsGourmet.Application.UseCases.V1.Admin.VendaPresencial.CriarVendaPresencial;

public record CriarVendaPresencialItemResponse(
    Guid ProdutoId,
    string ProdutoNome,
    int Quantidade,
    decimal PrecoUnitario,
    decimal Subtotal);

public record CriarVendaPresencialResponse(
    Guid PedidoId,
    decimal Total,
    IReadOnlyList<CriarVendaPresencialItemResponse> Itens,
    string Canal,
    Guid OperadorId,
    string ClienteNome,
    DateTime CriadoEm);
