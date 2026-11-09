using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Estoque;
using GatumsGourmet.Domain.Enums.Estoque;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Estoque.AdjustEstoque;

public record AdjustEstoqueRequest(TipoMovimentoEstoque Tipo, int Quantidade, Guid? UsuarioId = null, string? Observacao = null);
public record AdjustEstoqueResponse(int EstoqueAnterior, int EstoqueNovo);

public interface IAdjustEstoqueHandler
{
    Task<Result<AdjustEstoqueResponse>> Handle(Guid produtoId, AdjustEstoqueRequest request, CancellationToken ct = default);
}

/// <summary>
/// Ajusta o estoque de um produto (entrada/saída/ajuste) e registra o movimento
/// na tabela auxiliar de auditoria. Venda não é permitida por este endpoint —
/// o débito de venda acontece no checkout.
/// </summary>
public sealed class AdjustEstoqueHandler(
    IProdutoRepository produtos,
    IMovimentoEstoqueRepository movimentos,
    IClock clock) : IAdjustEstoqueHandler
{
    public async Task<Result<AdjustEstoqueResponse>> Handle(Guid produtoId, AdjustEstoqueRequest request, CancellationToken ct = default)
    {
        if (request.Tipo == TipoMovimentoEstoque.Venda)
            return Result<AdjustEstoqueResponse>.Fail("Use o checkout para débitos de venda. Tipos permitidos: Entrada, Saida, Ajuste.");

        if (request.Quantidade == 0)
            return Result<AdjustEstoqueResponse>.Fail("Quantidade não pode ser zero.");

        var produto = await produtos.GetByIdAsync(produtoId, ct);
        if (produto is null)
            return Result<AdjustEstoqueResponse>.Fail("Produto não encontrado.");

        var estoqueAnterior = produto.Estoque;
        int delta = request.Tipo switch
        {
            TipoMovimentoEstoque.Entrada => request.Quantidade,
            TipoMovimentoEstoque.Saida => -request.Quantidade,
            TipoMovimentoEstoque.Ajuste => request.Quantidade, // ajuste pode ser + ou -
            _ => 0
        };

        var novoEstoque = produto.Estoque + delta;
        if (novoEstoque < 0)
            return Result<AdjustEstoqueResponse>.Fail($"Estoque resultante ({novoEstoque}) não pode ser negativo.");

        produto.Estoque = novoEstoque;

        var movimento = new MovimentoEstoque
        {
            Id = Guid.NewGuid(),
            ProdutoId = produtoId,
            Tipo = request.Tipo,
            Quantidade = delta,
            SaldoResultante = novoEstoque,
            UsuarioId = request.UsuarioId,
            Observacao = request.Observacao,
            Data = clock.UtcNow
        };

        await movimentos.AddAsync(movimento, ct);
        await produtos.SaveChangesAsync(ct);
        await movimentos.SaveChangesAsync(ct);

        return Result<AdjustEstoqueResponse>.Ok(new AdjustEstoqueResponse(estoqueAnterior, novoEstoque));
    }
}
