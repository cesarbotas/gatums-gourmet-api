using GatumsGourmet.Application.UseCases.V1.Auth.Login;
using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using RefreshTokenEntity = GatumsGourmet.Domain.Entities.Auth.RefreshToken;

namespace GatumsGourmet.Application.UseCases.V1.Auth.RefreshToken;

public sealed class RefreshTokenHandler(
    IRefreshTokenStore store,
    IRefreshTokenGenerator gen,
    IJwtTokenGenerator jwt,
    IPermissionResolver permissions,
    IUsuarioRepository usuarios,
    IClock clock,
    IConfiguration config) : IRefreshTokenHandler
{
    public async Task<Result<LoginResponse>> Handle(RefreshTokenRequest request, string? ip, CancellationToken ct = default)
    {
        var hash = gen.Hash(request.RefreshToken);
        var token = await store.GetByHashAsync(hash, ct);

        if (token is null)
            return Result<LoginResponse>.Fail("Refresh token inválido.");

        var now = clock.UtcNow;

        // Detecção de reúso: um refresh já revogado reapresentado = sinal de roubo.
        // Revoga a família inteira e falha.
        if (token.Revogado)
        {
            await store.RevokeFamilyAsync(token.FamiliaId, "Reúso detectado", now, ct);
            return Result<LoginResponse>.Fail("Refresh token inválido.");
        }

        if (token.ExpiraEm <= now)
            return Result<LoginResponse>.Fail("Refresh token expirado.");

        var usuario = await usuarios.GetByIdAsync(token.UsuarioId, ct);
        if (usuario is null)
            return Result<LoginResponse>.Fail("Refresh token inválido.");

        if (!usuario.Ativo)
            return Result<LoginResponse>.Fail("Usuário inativo.");

        // Rotação: marca o token atual como revogado e cria o novo na mesma família.
        var (novoPlain, novoHash) = gen.Generate();

        token.Revogado = true;
        token.RevogadoEm = now;
        token.SubstituidoPorTokenHash = novoHash;
        token.MotivoRevogacao = "Rotacionado";

        var refreshDays = config.GetValue<int?>("Jwt:RefreshTokenDays") ?? 7;
        var novo = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UsuarioId = token.UsuarioId,
            TokenHash = novoHash,
            FamiliaId = token.FamiliaId,
            ExpiraEm = now.AddDays(refreshDays),
            CriadoEm = now,
            Revogado = false,
            CriadoPorIp = ip
        };
        await store.AddAsync(novo, ct);
        await store.SaveChangesAsync(ct);

        var (roles, permissoes) = await permissions.ResolveAsync(usuario.Id, ct);
        var accessToken = jwt.Generate(usuario.Id, usuario.Email, roles, permissoes);

        var accessMinutes = config.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 15;
        var expiresIn = accessMinutes * 60;

        var dto = new LoginUserDto(usuario.Id, usuario.Nome, usuario.Email, roles);
        return Result<LoginResponse>.Ok(new LoginResponse(accessToken, novoPlain, expiresIn, dto));
    }
}
