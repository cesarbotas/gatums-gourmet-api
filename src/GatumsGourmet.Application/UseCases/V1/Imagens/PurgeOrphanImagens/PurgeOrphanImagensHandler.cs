using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Application.UseCases.V1.Imagens.PurgeOrphanImagens;

/// <summary>
/// Lista os objetos do prefixo <c>produtos/</c>, cruza com as URLs em uso pelos
/// produtos e remove os que estão órfãos E foram modificados antes do início do
/// dia de hoje (UTC). A carência "início do dia" protege uploads recentes que
/// ainda não foram salvos em um produto (ex.: usuário subiu a foto e não concluiu).
/// </summary>
public sealed class PurgeOrphanImagensHandler(
    IImageStorage storage,
    IProdutoRepository produtos,
    IClock clock) : IPurgeOrphanImagensHandler
{
    private const string Prefixo = "produtos";

    public async Task<Result<int>> Handle(CancellationToken ct = default)
    {
        // Corte do grace: início do dia de hoje em UTC. Objetos modificados em
        // "hoje" (>= corte) nunca são apagados nesta execução.
        var corte = clock.UtcNow.Date;

        var objetos = await storage.ListObjectsAsync(Prefixo, ct);
        if (objetos.Count == 0)
            return Result<int>.Ok(0);

        var urlsEmUso = (await produtos.ListFotoUrlsEmUsoAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var removidos = 0;
        foreach (var obj in objetos)
        {
            var emUso = urlsEmUso.Contains(obj.Url);
            var dentroDaCarencia = obj.LastModifiedUtc >= corte;

            if (emUso || dentroDaCarencia)
                continue;

            await storage.DeleteAsync(obj.ObjectKey, ct);
            removidos++;
        }

        return Result<int>.Ok(removidos);
    }
}
