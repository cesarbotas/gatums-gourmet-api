using Xunit;

namespace GatumsGourmet.IntegrationTests.Factory;

/// <summary>
/// Compartilha uma única <see cref="CustomWebApplicationFactory"/> entre todas as classes
/// de teste de integração. Assim o docker compose sobe/derruba uma só vez para toda a suíte,
/// evitando subir/derrubar o PostgreSQL a cada classe.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    // Classe âncora apenas: não contém código.
}
