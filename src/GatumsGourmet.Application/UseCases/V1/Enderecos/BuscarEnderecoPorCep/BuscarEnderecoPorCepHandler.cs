using System.Text.RegularExpressions;
using GatumsGourmet.Application.UseCases.V1.Enderecos;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace GatumsGourmet.Application.UseCases.V1.Enderecos.BuscarEnderecoPorCep;

public interface IBuscarEnderecoPorCepHandler
{
    Task<Result<EnderecoCepResponse>> Handle(string cep, CancellationToken ct = default);
}

public sealed partial class BuscarEnderecoPorCepHandler(
    ICepProvider provider,
    ILogger<BuscarEnderecoPorCepHandler> logger) : IBuscarEnderecoPorCepHandler
{
    public async Task<Result<EnderecoCepResponse>> Handle(string cep, CancellationToken ct = default)
    {
        // Normaliza: remove tudo que não for dígito (aceita "01001-000" ou "01001000").
        var normalizado = NaoDigitos().Replace(cep ?? string.Empty, string.Empty);

        if (normalizado.Length != 8)
            return Result<EnderecoCepResponse>.Fail("CEP inválido.");

        EnderecoCep? endereco;
        try
        {
            endereco = await provider.BuscarAsync(normalizado, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao consultar o provedor de CEP para {Cep}.", normalizado);
            return Result<EnderecoCepResponse>.Fail("Serviço de CEP indisponível.");
        }

        if (endereco is null)
            return Result<EnderecoCepResponse>.Fail("CEP não encontrado.");

        return Result<EnderecoCepResponse>.Ok(new EnderecoCepResponse(
            endereco.Cep,
            endereco.Logradouro,
            endereco.Complemento,
            endereco.Bairro,
            endereco.Cidade,
            endereco.Estado));
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex NaoDigitos();
}
