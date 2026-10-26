using GatumsGourmet.Domain.Common.V1;
using GatumsGourmet.Domain.Repositories;

namespace GatumsGourmet.Application.UseCases.V1.Usuarios.DeleteUsuario;

public interface IDeleteUsuarioHandler
{
    Task<Result<bool>> Handle(Guid id, CancellationToken ct = default);
}

public sealed class DeleteUsuarioHandler(IUsuarioRepository usuarios) : IDeleteUsuarioHandler
{
    public async Task<Result<bool>> Handle(Guid id, CancellationToken ct = default)
    {
        var usuario = await usuarios.GetByIdComRolesAsync(id, ct);
        if (usuario is null) return Result<bool>.Fail("Usuário não encontrado.");

        // Soft-delete equivale a desativar: a regra "nunca sem admin" se aplica.
        // Não removemos a linha para preservar histórico (ex.: pedidos).
        if (usuario.Ativo
            && UsuarioMapper.TemRoleAdmin(usuario)
            && await usuarios.CountAdminsAtivosAsync(ct) <= 1)
        {
            return Result<bool>.Fail("O sistema deve ter ao menos um administrador ativo.");
        }

        await usuarios.SetAtivoAsync(id, false, ct);
        await usuarios.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}
