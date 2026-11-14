using GatumsGourmet.Domain.Enums.Pagamentos;

namespace GatumsGourmet.Application.UseCases.V1.Admin.VendaPresencial.RegistrarPagamentoPresencial;

/// <param name="PedidoId">Pedido presencial a ser pago.</param>
/// <param name="Metodo">Forma de pagamento presencial.</param>
/// <param name="ValorRecebido">Valor pago em espécie pelo cliente. Obrigatório quando Metodo=Dinheiro.</param>
/// <param name="PayerEmail">E-mail do cliente para notificação Pix (opcional).</param>
/// <param name="IdempotencyKey">Chave de idempotência gerada pelo cliente.</param>
public record RegistrarPagamentoPresencialRequest(
    Guid PedidoId,
    MetodoPagamentoPresencial Metodo,
    decimal? ValorRecebido = null,
    string? PayerEmail = null,
    string? IdempotencyKey = null);
