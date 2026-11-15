using FluentValidation;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarCartaoCredito;

public record ProcessarCartaoCreditoRequest(
    Guid PedidoId,
    decimal? Valor,
    string Numero,
    string Nome,
    string Validade,
    string Cvv,
    int Parcelas = 1);

public class ProcessarCartaoCreditoValidator : AbstractValidator<ProcessarCartaoCreditoRequest>
{
    public ProcessarCartaoCreditoValidator()
    {
        RuleFor(x => x.PedidoId).NotEmpty().WithMessage("PedidoId é obrigatório.");
        RuleFor(x => x.Numero).NotEmpty().CreditCard().WithMessage("Número de cartão de crédito inválido.");
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Nome do titular é obrigatório.");
        RuleFor(x => x.Validade).NotEmpty().Matches(@"^(0[1-9]|1[0-2])\/?([0-9]{2}|[0-9]{4})$").WithMessage("Validade inválida (MM/AA).");
        RuleFor(x => x.Cvv).NotEmpty().Length(3, 4).WithMessage("CVV deve ter 3 ou 4 dígitos.");
        RuleFor(x => x.Parcelas).GreaterThan(0).LessThanOrEqualTo(12).WithMessage("Parcelas devem estar entre 1 e 12.");
    }
}

public interface IProcessarCartaoCreditoHandler
{
    Task<Result<TransacaoResponse>> Handle(ProcessarCartaoCreditoRequest request, CancellationToken ct = default);
}

public sealed class ProcessarCartaoCreditoHandler(
    ITransacaoRepository transacoes,
    IPedidoRepository pedidos,
    IPaymentGateway gateway,
    IClock clock,
    ILogger<ProcessarCartaoCreditoHandler> logger) : IProcessarCartaoCreditoHandler
{
    public async Task<Result<TransacaoResponse>> Handle(ProcessarCartaoCreditoRequest request, CancellationToken ct = default)
    {
        var pedido = await pedidos.GetByIdAsync(request.PedidoId, ct);
        if (pedido is null) return Result<TransacaoResponse>.Fail("Pedido não encontrado.");

        var valor = request.Valor is > 0 ? request.Valor.Value : pedido.Total;

        var transacao = new TransacaoPagamento
        {
            Id = Guid.NewGuid(),
            PedidoId = request.PedidoId,
            Metodo = MetodoPagamento.CartaoCredito,
            Valor = valor,
            Status = StatusTransacao.Pendente,
            Operadora = "gateway_cartao",
            CriadoEm = clock.UtcNow,
            AtualizadoEm = clock.UtcNow
        };

        var resultado = await gateway.ProcessarAsync(
            transacao.Id,
            valor,
            MetodoPagamento.CartaoCredito,
            new CartaoInput(request.Numero, request.Nome, request.Validade, request.Cvv),
            ct);

        transacao.Status = resultado.Aprovado ? StatusTransacao.Aprovado : StatusTransacao.Recusado;
        transacao.OperadoraRef = resultado.Referencia;
        transacao.CartaoUltimos4 = resultado.Ultimos4;
        transacao.CartaoBandeira = resultado.Bandeira;
        transacao.MensagemErro = resultado.Erro;
        transacao.AtualizadoEm = clock.UtcNow;

        await transacoes.AddAsync(transacao, ct);
        await transacoes.SaveChangesAsync(ct);

        logger.LogInformation("Cartão de Crédito processado. TransacaoId={Id} PedidoId={PedidoId} Aprovado={Aprovado}",
            transacao.Id, request.PedidoId, resultado.Aprovado);

        return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(transacao));
    }
}

