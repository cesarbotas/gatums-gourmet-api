using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Enderecos.BuscarEnderecoPorCep;

public interface IBuscarEnderecoPorCepHandler
{
    Task<Result<EnderecoCepResponse>> Handle(string cep, CancellationToken ct = default);
}
