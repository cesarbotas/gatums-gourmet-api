using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Entities.Auth;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace GatumsGourmet.Application.UseCases.V1.Auth.Login;

public sealed class LoginHandler(
    IUsuarioRepository usuarios,
    IPasswordHasher hasher,
    IJwtTokenGenerator jwt,
    IRefreshTokenGenerator refreshGen,
    IRefreshTokenStore refreshStore,
    IPermissionResolver permissions,
    IClock clock,
    IConfiguration config) : ILoginHandler
{
    public async Task<Result<LoginResponse>> Handle(LoginRequest request, CancellationToken ct = default)
    {
        var usuario = await usuarios.GetByEmailAsync(request.Email, ct);
        if (usuario is null || !hasher.Verify(request.Senha, usuario.SenhaHash))
            return Result<LoginResponse>.Fail("Credenciais inválidas.");

        if (!usuario.Ativo)
            return Result<LoginResponse>.Fail("Usuário inativo.");

        var (roles, permissoes) = await permissions.ResolveAsync(usuario.Id, ct);

        var accessToken = jwt.Generate(usuario.Id, usuario.Email, roles, permissoes);

        var (plain, hash) = refreshGen.Generate();
        var refreshDays = config.GetValue<int?>("Jwt:RefreshTokenDays") ?? 7;
        var now = clock.UtcNow;
        var token = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UsuarioId = usuario.Id,
            TokenHash = hash,
            FamiliaId = Guid.NewGuid(),
            ExpiraEm = now.AddDays(refreshDays),
            CriadoEm = now,
            Revogado = false
        };
        await refreshStore.AddAsync(token, ct);
        await refreshStore.SaveChangesAsync(ct);

        var accessMinutes = config.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 15;
        var expiresIn = accessMinutes * 60;

        var dto = new LoginUserDto(usuario.Id, usuario.Nome, usuario.Email, roles);
        return Result<LoginResponse>.Ok(new LoginResponse(accessToken, plain, expiresIn, dto));
    }
}
