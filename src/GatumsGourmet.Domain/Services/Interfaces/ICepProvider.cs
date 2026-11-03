namespace GatumsGourmet.Domain.Services.Interfaces;

/// <summary>
/// Abstração de um provedor externo de consulta de endereço por CEP
/// (ex.: ViaCEP). A implementação vive na Infrastructure.
/// </summary>
public interface ICepProvider
{
    /// <summary>
    /// Busca o endereço de um CEP (8 dígitos, sem máscara). Retorna
    /// <c>null</c> quando o CEP não existe na base do provedor.
    /// Lança em caso de falha de comunicação/timeout.
    /// </summary>
    Task<EnderecoCep?> BuscarAsync(string cep, CancellationToken ct = default);
}

/// <summary>
/// Endereço retornado por um provedor de CEP. Campos que o provedor não
/// fornece (número) ficam a cargo do usuário no preenchimento.
/// </summary>
public record EnderecoCep(
    string Cep,
    string Logradouro,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Estado);
