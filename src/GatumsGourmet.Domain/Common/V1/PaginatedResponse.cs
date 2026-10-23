namespace GatumsGourmet.Domain.Common.V1;

/// <summary>
/// Envelope padrão de respostas paginadas (campos em português).
/// Os campos derivados (TotalPaginas, TemPaginaAnterior, TemProximaPagina) são
/// calculados a partir de TotalRegistros, PaginaAtual e TamanhoPagina — o
/// consumidor (front) não precisa calcular nada.
/// </summary>
public record PaginatedResponse<T>
{
    /// <summary>Itens da página atual.</summary>
    public IReadOnlyList<T> Itens { get; init; }

    /// <summary>Página atual (1-based).</summary>
    public int PaginaAtual { get; init; }

    /// <summary>Quantidade de itens por página.</summary>
    public int TamanhoPagina { get; init; }

    /// <summary>Total de registros encontrados (não de páginas).</summary>
    public int TotalRegistros { get; init; }

    /// <summary>Total de páginas, calculado a partir de TotalRegistros e TamanhoPagina.</summary>
    public int TotalPaginas { get; init; }

    /// <summary>Indica se existe página anterior.</summary>
    public bool TemPaginaAnterior { get; init; }

    /// <summary>Indica se existe próxima página.</summary>
    public bool TemProximaPagina { get; init; }

    public PaginatedResponse(IReadOnlyList<T> itens, int totalRegistros, int paginaAtual, int tamanhoPagina)
    {
        Itens = itens;
        TotalRegistros = totalRegistros < 0 ? 0 : totalRegistros;
        PaginaAtual = paginaAtual < 1 ? 1 : paginaAtual;
        TamanhoPagina = tamanhoPagina < 1 ? 1 : tamanhoPagina;
        TotalPaginas = TotalRegistros == 0 ? 0 : (int)Math.Ceiling(TotalRegistros / (double)TamanhoPagina);
        TemPaginaAnterior = PaginaAtual > 1;
        TemProximaPagina = PaginaAtual < TotalPaginas;
    }
}
