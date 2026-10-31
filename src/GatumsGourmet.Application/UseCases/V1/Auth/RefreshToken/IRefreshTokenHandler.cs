using GatumsGourmet.Application.UseCases.V1.Auth.Login;
using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Auth.RefreshToken;

public interface IRefreshTokenHandler
{
    Task<Result<LoginResponse>> Handle(RefreshTokenRequest request, string? ip, CancellationToken ct = default);
}
