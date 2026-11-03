using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Auth.Logout;

public interface ILogoutHandler
{
    Task<Result<bool>> Handle(Guid usuarioId, LogoutRequest request, CancellationToken ct = default);
}
