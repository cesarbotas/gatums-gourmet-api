using System.Net.Http.Json;
using System.Text.Json.Serialization;
using GatumsGourmet.Domain.Services.Interfaces;

namespace GatumsGourmet.Infrastructure.Services.Enderecos;

/// <summary>
/// Implementação de <see cref="ICepProvider"/> usando o ViaCEP
/// (https://viacep.com.br/ws/{cep}/json/). O <see cref="HttpClient"/> é
/// injetado via IHttpClientFactory (ver registro em AddInfrastructure),
/// com a base URL e o timeout configurados lá.
/// </summary>
public sealed class ViaCepProvider(HttpClient http) : ICepProvider
{
    public async Task<EnderecoCep?> BuscarAsync(string cep, CancellationToken ct = default)
    {
        // Espera-se o CEP já normalizado (8 dígitos) pelo handler.
        var dto = await http.GetFromJsonAsync<ViaCepResponse>($"{cep}/json/", ct);

        // ViaCEP responde 200 com { "erro": true } quando o CEP não existe.
        if (dto is null || dto.Erro || string.IsNullOrWhiteSpace(dto.Cep))
            return null;

        return new EnderecoCep(
            Cep: SomenteDigitos(dto.Cep),
            Logradouro: dto.Logradouro ?? string.Empty,
            Complemento: string.IsNullOrWhiteSpace(dto.Complemento) ? null : dto.Complemento,
            Bairro: dto.Bairro ?? string.Empty,
            Cidade: dto.Localidade ?? string.Empty,
            Estado: dto.Uf ?? string.Empty);
    }

    private static string SomenteDigitos(string s) => new(s.Where(char.IsDigit).ToArray());

    /// <summary>Shape do JSON do ViaCEP (apenas os campos usados).</summary>
    private sealed record ViaCepResponse
    {
        [JsonPropertyName("cep")] public string? Cep { get; init; }
        [JsonPropertyName("logradouro")] public string? Logradouro { get; init; }
        [JsonPropertyName("complemento")] public string? Complemento { get; init; }
        [JsonPropertyName("bairro")] public string? Bairro { get; init; }
        [JsonPropertyName("localidade")] public string? Localidade { get; init; }
        [JsonPropertyName("uf")] public string? Uf { get; init; }
        [JsonPropertyName("erro")] public bool Erro { get; init; }
    }
}
