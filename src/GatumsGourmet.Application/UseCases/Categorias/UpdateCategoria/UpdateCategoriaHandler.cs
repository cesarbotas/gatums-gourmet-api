using FluentValidation;
using GatumsGourmet.Application.Common;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.Categorias.UpdateCategoria;

public record UpdateCategoriaRequest(long Id, string Nome, string? Descricao, bool Ativo);

public class UpdateCategoriaValidator : AbstractValidator<UpdateCategoriaRequest>
{
    public UpdateCategoriaValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Descricao).MaximumLength(500);
    }
}

public interface IUpdateCategoriaHandler
{
    Task<Result<bool>> Handle(UpdateCategoriaRequest request, CancellationToken ct = default);
}

public sealed class UpdateCategoriaHandler(ICategoriaRepository categorias) : IUpdateCategoriaHandler
{
    public async Task<Result<bool>> Handle(UpdateCategoriaRequest request, CancellationToken ct = default)
    {
        var categoria = await categorias.GetByIdAsync(request.Id, ct);
        if (categoria is null) return Result<bool>.Fail("Categoria não encontrada.");

        if (await categorias.ExistsByNomeAsync(request.Nome, request.Id, ct))
            return Result<bool>.Fail("Já existe uma categoria com este nome.");

        categoria.Nome = request.Nome;
        categoria.Descricao = request.Descricao;
        categoria.Ativo = request.Ativo;
        await categorias.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}

