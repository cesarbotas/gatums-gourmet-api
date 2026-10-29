using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Imagens.PurgeOrphanImagens;

/// <summary>
/// Caso de uso de limpeza de imagens órfãs no storage: remove objetos do
/// prefixo <c>produtos/</c> que não são mais referenciados por nenhum produto
/// e que foram modificados antes do início do dia de hoje (período de carência).
/// Retorna a quantidade de objetos removidos.
/// </summary>
public interface IPurgeOrphanImagensHandler
{
    Task<Result<int>> Handle(CancellationToken ct = default);
}
