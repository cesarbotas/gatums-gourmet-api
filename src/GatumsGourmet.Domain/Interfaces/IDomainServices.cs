using GatumsGourmet.Domain.Entities;
using GatumsGourmet.Domain.Enums;

namespace GatumsGourmet.Domain.Interfaces;

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IPedidoWorkflowService
{
    void ValidarTransicao(PedidoEtapa atual, PedidoEtapa proxima, PedidoActor actor);
}
