using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Auth.Login;

public interface ILoginHandler
{
    Task<Result<LoginResponse>> Handle(LoginRequest request, CancellationToken ct = default);
}
