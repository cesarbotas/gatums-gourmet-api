using GatumsGourmet.Application.Common;
using GatumsGourmet.Application.Interfaces;
using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Enums.Produtos;
using GatumsGourmet.Domain.Exceptions;
using GatumsGourmet.Domain.Interfaces;

namespace GatumsGourmet.Application.UseCases.Produtos.CreateProduto;

public sealed class CreateProdutoHandler(IProdutoRepository produtos, IClock clock) : ICreateProdutoHandler
{
    public async Task<Result<CreateProdutoResponse>> Handle(CreateProdutoRequest request, CancellationToken ct = default)
    {
        if (request.Tipo == TipoProduto.Combo && (request.Itens is null || request.Itens.Count == 0))
            throw new BusinessRuleException("Combo exige ao menos um item.");

        var produto = new Produto
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Descricao = request.Descricao,
            Categoria = request.Categoria,
            Preco = request.Preco,
            FotoUrl = request.FotoUrl,
            Disponivel = request.Disponivel,
            Tipo = request.Tipo,
            CriadoEm = clock.UtcNow
        };

        if (request.Tipo == TipoProduto.Combo)
            foreach (var item in request.Itens!)
                produto.ComboItens.Add(new ProdutoComboItem { Id = Guid.NewGuid(), ComboProdutoId = produto.Id, ProdutoId = item.ProdutoId, Quantidade = item.Quantidade });

        await produtos.AddAsync(produto, ct);
        await produtos.SaveChangesAsync(ct);
        return Result<CreateProdutoResponse>.Ok(new CreateProdutoResponse(produto.Id));
    }
}
