using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Auth.LogoutAll;

public interface ILogoutAllHandler
{
    Task<Result<bool>> Handle(Guid usuarioId, CancellationToken ct = default);
}
