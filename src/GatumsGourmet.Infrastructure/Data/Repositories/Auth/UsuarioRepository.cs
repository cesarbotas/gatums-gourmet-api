using GatumsGourmet.Domain.Repositories;
using GatumsGourmet.Domain.Services.Interfaces;
using GatumsGourmet.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;

namespace GatumsGourmet.Infrastructure.Data.Repositories.Auth;

public class UsuarioRepository(GatumsDbContext db) : IUsuarioRepository
{
    public Task<Usuario?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<Usuario?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task AddAsync(Usuario usuario, CancellationToken ct = default) =>
        await db.Usuarios.AddAsync(usuario, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<Usuario>> ListAsync(CancellationToken ct = default) =>
        await db.Usuarios
            .AsNoTracking()
            .Include(u => u.UsuarioRoles)
            .ThenInclude(ur => ur.Role)
            .OrderBy(u => u.Nome)
            .ToListAsync(ct);

    public Task<Usuario?> GetByIdComRolesAsync(Guid id, CancellationToken ct = default) =>
        db.Usuarios
            .Include(u => u.UsuarioRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task SetRolesAsync(Guid usuarioId, IEnumerable<int> roleIds, CancellationToken ct = default)
    {
        var atuais = await db.UsuarioRoles
            .Where(ur => ur.UsuarioId == usuarioId)
            .ToListAsync(ct);

        db.UsuarioRoles.RemoveRange(atuais);

        var novos = roleIds
            .Select(rid => new UsuarioRole { UsuarioId = usuarioId, RoleId = rid });

        await db.UsuarioRoles.AddRangeAsync(novos, ct);
    }

    public async Task SetAtivoAsync(Guid usuarioId, bool ativo, CancellationToken ct = default)
    {
        var usuario = await db.Usuarios.FirstOrDefaultAsync(u => u.Id == usuarioId, ct);
        if (usuario is not null)
        {
            usuario.Ativo = ativo;
        }
    }

    public Task<int> CountAdminsAtivosAsync(CancellationToken ct = default) =>
        db.UsuarioRoles.CountAsync(ur => ur.Role.Nome == "Admin" && ur.Usuario.Ativo, ct);
}
