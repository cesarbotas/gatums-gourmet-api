using FluentValidation;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Entities.Produtos;

namespace GatumsGourmet.Application.UseCases.V1.Categorias.CreateCategoria;

public sealed class CreateCategoriaHandler(ICategoriaRepository categorias) : ICreateCategoriaHandler
{
    public async Task<Result<CreateCategoriaResponse>> Handle(CreateCategoriaRequest request, CancellationToken ct = default)
    {
        if (await categorias.ExistsByNomeAsync(request.Nome, null, ct))
            return Result<CreateCategoriaResponse>.Fail("Já existe uma categoria com este nome.");

        var categoria = new Categoria
        {
            Nome = request.Nome,
            Descricao = request.Descricao,
            Ativo = request.Ativo
        };

        await categorias.AddAsync(categoria, ct);
        await categorias.SaveChangesAsync(ct);
        return Result<CreateCategoriaResponse>.Ok(new CreateCategoriaResponse(categoria.Id));
    }
}
