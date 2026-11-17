using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Clientes.UpdateCliente;

public interface IUpdateClienteHandler
{
    Task<Result<bool>> Handle(UpdateClienteRequest request, CancellationToken ct = default);
}
