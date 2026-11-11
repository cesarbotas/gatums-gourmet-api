using FluentValidation;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Pagamentos;
using GatumsGourmet.Domain.Enums.Pagamentos;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V1.Pagamentos.ProcessarCartaoDebito;

public record ProcessarCartaoDebitoRequest(
    Guid PedidoId,
    decimal? Valor,
    string Numero,
    string Nome,
    string Validade,
    string Cvv);

public class ProcessarCartaoDebitoValidator : AbstractValidator<ProcessarCartaoDebitoRequest>
{
    public ProcessarCartaoDebitoValidator()
    {
        RuleFor(x => x.PedidoId).NotEmpty().WithMessage("PedidoId é obrigatório.");
        RuleFor(x => x.Numero).NotEmpty().CreditCard().WithMessage("Número de cartão de débito inválido.");
        RuleFor(x => x.Nome).NotEmpty().WithMessage("Nome do titular é obrigatório.");
        RuleFor(x => x.Validade).NotEmpty().Matches(@"^(0[1-9]|1[0-2])\/?([0-9]{2}|[0-9]{4})$").WithMessage("Validade inválida (MM/AA).");
        RuleFor(x => x.Cvv).NotEmpty().Length(3, 4).WithMessage("CVV deve ter 3 ou 4 dígitos.");
    }
}

public interface IProcessarCartaoDebitoHandler
{
    Task<Result<TransacaoResponse>> Handle(ProcessarCartaoDebitoRequest request, CancellationToken ct = default);
}

public sealed class ProcessarCartaoDebitoHandler(
    ITransacaoRepository transacoes,
    IPedidoRepository pedidos,
    IPaymentGateway gateway,
    IClock clock,
    ILogger<ProcessarCartaoDebitoHandler> logger) : IProcessarCartaoDebitoHandler
{
    public async Task<Result<TransacaoResponse>> Handle(ProcessarCartaoDebitoRequest request, CancellationToken ct = default)
    {
        var pedido = await pedidos.GetByIdAsync(request.PedidoId, ct);
        if (pedido is null) return Result<TransacaoResponse>.Fail("Pedido não encontrado.");

        var valor = request.Valor is > 0 ? request.Valor.Value : pedido.Total;

        var transacao = new TransacaoPagamento
        {
            Id = Guid.NewGuid(),
            PedidoId = request.PedidoId,
            Metodo = MetodoPagamento.CartaoDebito,
            Valor = valor,
            Status = StatusTransacao.Pendente,
            Operadora = "gateway_debito",
            CriadoEm = clock.UtcNow,
            AtualizadoEm = clock.UtcNow
        };

        var resultado = await gateway.ProcessarAsync(
            transacao.Id,
            valor,
            MetodoPagamento.CartaoDebito,
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

        logger.LogInformation("Cartão de Débito processado. TransacaoId={Id} PedidoId={PedidoId} Aprovado={Aprovado}",
            transacao.Id, request.PedidoId, resultado.Aprovado);

        return Result<TransacaoResponse>.Ok(TransacaoMapper.Map(transacao));
    }
}

