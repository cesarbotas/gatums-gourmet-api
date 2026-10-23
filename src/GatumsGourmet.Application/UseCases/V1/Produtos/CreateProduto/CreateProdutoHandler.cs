using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Entities.Produtos;
using GatumsGourmet.Domain.Enums.Produtos;
using GatumsGourmet.Domain.Exceptions;

namespace GatumsGourmet.Application.UseCases.V1.Produtos.CreateProduto;

public sealed class CreateProdutoHandler(IProdutoRepository produtos, ICategoriaRepository categorias, IClock clock) : ICreateProdutoHandler
{
    public async Task<Result<CreateProdutoResponse>> Handle(CreateProdutoRequest request, CancellationToken ct = default)
    {
        if (request.Tipo == TipoProduto.Combo && (request.Itens is null || request.Itens.Count == 0))
            throw new BusinessRuleException("Combo exige ao menos um item.");

        var categoria = await categorias.GetByIdAsync(request.CategoriaId, ct);
        if (categoria is null) return Result<CreateProdutoResponse>.Fail("Categoria não encontrada.");

        var produto = new Produto
        {
            Id = Guid.NewGuid(),
            Nome = request.Nome,
            Descricao = request.Descricao,
            CategoriaId = request.CategoriaId,
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

