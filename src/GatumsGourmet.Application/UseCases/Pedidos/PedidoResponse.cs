namespace GatumsGourmet.Application.UseCases.Pedidos;

public record PedidoItemResponse(Guid ProdutoId, string Nome, decimal Preco, string Foto, int Quantidade);
public record TimelineEventoResponse(string Etapa, DateTime Em, string Por, string? Observacao, string? EntregaTipo);
public record PedidoPagamentoResponse(Guid TransacaoId, string Resumo, DateTime AprovadoEm);

public record PedidoResponse(
    Guid Id,
    Guid UserId,
    string ClienteNome,
    IReadOnlyList<PedidoItemResponse> Itens,
    decimal Total,
    DateTime Data,
    string StatusAtual,
    IReadOnlyList<TimelineEventoResponse> Timeline,
    PedidoPagamentoResponse? Pagamento);
