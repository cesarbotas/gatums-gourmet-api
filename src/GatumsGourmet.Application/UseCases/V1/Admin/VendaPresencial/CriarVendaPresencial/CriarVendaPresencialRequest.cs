namespace GatumsGourmet.Application.UseCases.V1.Admin.VendaPresencial.CriarVendaPresencial;

public record CriarVendaPresencialItemRequest(Guid ProdutoId, int Quantidade);

/// <param name="ClienteNome">Nome para identificação. Se omitido, registrado como "Balcão".</param>
/// <param name="OperadorId">UserId do admin extraído do JWT — nunca confiar no body.</param>
/// <param name="Itens">Lista de produtos e quantidades.</param>
/// <param name="IdempotencyKey">Chave única gerada pelo cliente para evitar duplicatas.</param>
public record CriarVendaPresencialRequest(
    string? ClienteNome,
    Guid OperadorId,
    IReadOnlyList<CriarVendaPresencialItemRequest> Itens,
    string? IdempotencyKey = null);
