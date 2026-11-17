using GatumsGourmet.Domain.Enums.Estoque;

namespace GatumsGourmet.Application.UseCases.V1.Estoque.AdjustEstoque;

public record AdjustEstoqueRequest(
    TipoMovimentoEstoque Tipo,
    int Quantidade,
    Guid? UsuarioId = null,
    string? Observacao = null
);