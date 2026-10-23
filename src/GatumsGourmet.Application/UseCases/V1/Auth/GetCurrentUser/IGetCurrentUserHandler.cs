using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Auth.GetCurrentUser;

public interface IGetCurrentUserHandler
{
    Task<Result<GetCurrentUserResponse>> Handle(Guid userId, CancellationToken ct = default);
}
