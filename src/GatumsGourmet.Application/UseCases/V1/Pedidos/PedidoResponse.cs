using GatumsGourmet.Domain.Enums.Pedidos;

namespace GatumsGourmet.Application.UseCases.V1.Pedidos;

public record PedidoItemResponse(Guid ProdutoId, string Nome, decimal Preco, string Foto, int Quantidade);

public record TimelineEventoResponse(
    PedidoEtapa Etapa,
    DateTime Em,
    PedidoActor Por,
    string? Observacao,
    EntregaTipo? EntregaTipo);

public record PedidoPagamentoResponse(Guid TransacaoId, string Resumo, DateTime AprovadoEm);

public record PedidoResponse(
    Guid Id,
    Guid UserId,
    string ClienteNome,
    IReadOnlyList<PedidoItemResponse> Itens,
    decimal Total,
    DateTime Data,
    PedidoEtapa StatusAtual,
    CanalVenda Canal,
    IReadOnlyList<TimelineEventoResponse> Timeline,
    PedidoPagamentoResponse? Pagamento);
