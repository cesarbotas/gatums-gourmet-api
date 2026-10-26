using GatumsGourmet.Api.Endpoints.V1;

namespace GatumsGourmet.Api.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapGatumsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapV1Endpoints();

        // Quando existir uma V2, basta criar os endpoints em Endpoints/V2/ e
        // adicionar aqui: app.MapV2Endpoints();
        // A V2 mapeia apenas os módulos que mudaram e reaproveita a V1 no resto.

        return app;
    }

    /// <summary>
    /// Mapeia todos os endpoints da versão 1 sob o prefixo <c>/api/v1</c>.
    /// </summary>
    private static void MapV1Endpoints(this IEndpointRouteBuilder app)
    {
        var v1 = app.MapGroup("/api/v1");

        AuthEndpoints.Map(v1.MapGroup("/auth").WithTags("Auth"));
        ProdutosEndpoints.Map(v1.MapGroup("/produtos").WithTags("Produtos"));
        CategoriasEndpoints.Map(v1.MapGroup("/categorias").WithTags("Categorias"));
        ClientesEndpoints.Map(v1.MapGroup("/clientes").WithTags("Clientes"));
        CarrinhoEndpoints.Map(v1.MapGroup("/carrinho").WithTags("Carrinho"));
        PedidosEndpoints.Map(v1.MapGroup("/pedidos").WithTags("Pedidos"));
        PagamentosEndpoints.Map(v1.MapGroup("/pagamentos").WithTags("Pagamentos"));
        RelatoriosEndpoints.Map(v1.MapGroup("/relatorios").WithTags("Relatorios"));
        RolesEndpoints.Map(v1.MapGroup("/roles").WithTags("Roles"));
        PermissionsEndpoints.Map(v1.MapGroup("/permissions").WithTags("Permissions"));
    }
}
