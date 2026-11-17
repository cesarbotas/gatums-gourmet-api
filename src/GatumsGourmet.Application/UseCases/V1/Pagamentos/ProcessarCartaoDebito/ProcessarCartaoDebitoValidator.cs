using FluentValidation;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarCartaoDebito;

public class ProcessarCartaoDebitoValidator : AbstractValidator<ProcessarCartaoDebitoRequest>
{
    public ProcessarCartaoDebitoValidator()
    {
        RuleFor(x => x.PedidoId)
            .NotEmpty()
            .WithMessage("PedidoId é obrigatório.");

        RuleFor(x => x.Numero)
            .NotEmpty()
            .CreditCard()
            .WithMessage("Número de cartão de débito inválido.");

        RuleFor(x => x.Nome)
            .NotEmpty()
            .WithMessage("Nome do titular é obrigatório.");

        RuleFor(x => x.Validade)
            .NotEmpty()
            .Matches(@"^(0[1-9]|1[0-2])\/?([0-9]{2}|[0-9]{4})$")
            .WithMessage("Validade inválida (MM/AA).");

        RuleFor(x => x.Cvv)
            .NotEmpty()
            .Length(3, 4)
            .WithMessage("CVV deve ter 3 ou 4 dígitos.");
    }
}