using GatumsGourmet.Application.Common;

namespace GatumsGourmet.Application.UseCases.Auth.RegisterUser;

public interface IRegisterUserHandler
{
    Task<Result<RegisterUserResponse>> Handle(RegisterUserRequest request, CancellationToken ct = default);
}
