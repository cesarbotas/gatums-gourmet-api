using FluentValidation;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.IniciarPagamento;

public class IniciarPagamentoValidator : AbstractValidator<IniciarPagamentoRequest>
{
    public IniciarPagamentoValidator()
    {
        RuleFor(x => x.PedidoId)
            .NotEmpty();

        RuleFor(x => x.Valor)
            .GreaterThan(0);
    }
}