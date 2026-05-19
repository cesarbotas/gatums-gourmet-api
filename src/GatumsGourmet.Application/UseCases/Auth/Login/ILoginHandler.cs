using GatumsGourmet.Application.Common;

namespace GatumsGourmet.Application.UseCases.Auth.Login;

public interface ILoginHandler
{
    Task<Result<LoginResponse>> Handle(LoginRequest request, CancellationToken ct = default);
}
