using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Usuarios.SetUsuarioAtivo;

public record SetUsuarioAtivoRequest(Guid UsuarioId, bool Ativo);

public interface ISetUsuarioAtivoHandler
{
    Task<Result<bool>> Handle(SetUsuarioAtivoRequest request, CancellationToken ct = default);
}

public sealed class SetUsuarioAtivoHandler(IUsuarioRepository usuarios) : ISetUsuarioAtivoHandler
{
    public async Task<Result<bool>> Handle(SetUsuarioAtivoRequest request, CancellationToken ct = default)
    {
        var usuario = await usuarios.GetByIdComRolesAsync(request.UsuarioId, ct);
        if (usuario is null) return Result<bool>.Fail("Usuário não encontrado.");

        // Regra "nunca sem admin": só é possível desativar um admin ativo se
        // ele não for o último admin ativo do sistema.
        if (!request.Ativo
            && usuario.Ativo
            && UsuarioMapper.TemRoleAdmin(usuario)
            && await usuarios.CountAdminsAtivosAsync(ct) <= 1)
        {
            return Result<bool>.Fail("O sistema deve ter ao menos um administrador ativo.");
        }

        await usuarios.SetAtivoAsync(request.UsuarioId, request.Ativo, ct);
        await usuarios.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}
