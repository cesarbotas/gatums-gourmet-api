using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Clientes.CreateCliente;

public interface ICreateClienteHandler
{
    Task<Result<CreateClienteResponse>> Handle(CreateClienteRequest request, CancellationToken ct = default);
}
