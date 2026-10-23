using GatumsGourmet.Domain.Common.V1;

namespace GatumsGourmet.Application.UseCases.V1.Auth.RegisterUser;

public interface IRegisterUserHandler
{
    Task<Result<RegisterUserResponse>> Handle(RegisterUserRequest request, CancellationToken ct = default);
}
