using FluentValidation;
using GatumsGourmet.Application.Common;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Entities.Produtos;

namespace GatumsGourmet.Application.UseCases.Categorias.CreateCategoria;

public record CreateCategoriaRequest(string Nome, string? Descricao, bool Ativo = true);
public record CreateCategoriaResponse(long Id);

public class CreateCategoriaValidator : AbstractValidator<CreateCategoriaRequest>
{
    public CreateCategoriaValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Descricao).MaximumLength(500);
    }
}

public interface ICreateCategoriaHandler
{
    Task<Result<CreateCategoriaResponse>> Handle(CreateCategoriaRequest request, CancellationToken ct = default);
}

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

