using FluentValidation;
using GatumsGourmet.Domain.Enums.Pagamentos;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPagamento;

public record IniciarPagamentoRequest(
    Guid PedidoId,
    MetodoPagamento Metodo,
    decimal Valor
);