using GatumsGourmet.Application.Common;

namespace GatumsGourmet.Application.UseCases.Auth.GetCurrentUser;

public interface IGetCurrentUserHandler
{
    Task<Result<GetCurrentUserResponse>> Handle(Guid userId, CancellationToken ct = default);
}
