using System.Security.Claims;
using GatumsGourmet.Api.Extensions;
using GatumsGourmet.Application.UseCases.V1.Auth.GetCurrentUser;
using GatumsGourmet.Application.UseCases.V1.Auth.Login;
using GatumsGourmet.Application.UseCases.V1.Auth.RegisterUser;
using Microsoft.AspNetCore.Mvc;

namespace GatumsGourmet.Api.Endpoints.V1;

public static class AuthEndpoints
{
    public static void Map(RouteGroupBuilder group)
    {
        group.MapPost("/login", async ([FromBody] LoginRequest request, ILoginHandler handler, CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult());

        group.MapPost("/register", async ([FromBody] RegisterUserRequest request, IRegisterUserHandler handler, CancellationToken ct) =>
            (await handler.Handle(request, ct)).ToHttpResult(StatusCodes.Status201Created));

        group.MapGet("/me", async (ClaimsPrincipal user, IGetCurrentUserHandler handler, CancellationToken ct) =>
            (await handler.Handle(user.GetUserId(), ct)).ToHttpResult())
            .RequireAuthorization();

        group.MapPost("/logout", (ClaimsPrincipal _) => Results.Ok())
            .RequireAuthorization();
    }
}
