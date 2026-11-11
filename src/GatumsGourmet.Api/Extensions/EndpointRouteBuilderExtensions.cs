using GatumsGourmet.Api.Endpoints.V1;
using GatumsGourmet.Api.Endpoints.V1.Categorias;
using GatumsGourmet.Api.Endpoints.V1.Clientes;
using GatumsGourmet.Api.Endpoints.V1.Estoque;
using GatumsGourmet.Api.Endpoints.V1.Imagens;
using GatumsGourmet.Api.Endpoints.V1.Pedidos;
using GatumsGourmet.Api.Endpoints.V1.Produtos;
using GatumsGourmet.Api.Endpoints.V1.Relatorios;
using PagamentosEndpointsV2 = GatumsGourmet.Api.Endpoints.V2.PagamentosEndpoints;

namespace GatumsGourmet.Api.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapGatumsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapV1Endpoints();

        // A V2 mapeia apenas os módulos que mudaram (hoje: pagamentos) e reaproveita
        // a V1 no resto. Novos módulos V2 entram em Endpoints/V2/ e em MapV2Endpoints.
        app.MapV2Endpoints();

        return app;
    }

    /// <summary>
    /// Mapeia os endpoints da versão 2 sob o prefixo <c>/api/v2</c> (apenas módulos alterados).
    /// </summary>
    private static void MapV2Endpoints(this IEndpointRouteBuilder app)
    {
        var v2 = app.MapGroup("/api/v2");

        PagamentosEndpointsV2.Map(v2.MapGroup("/pagamentos").WithTags("Pagamentos V2"));
    }

    /// <summary>
    /// Mapeia todos os endpoints da versão 1 sob o prefixo <c>/api/v1</c>.
    /// </summary>
    private static void MapV1Endpoints(this IEndpointRouteBuilder app)
    {
        var v1 = app.MapGroup("/api/v1");

        AuthEndpoints.Map(v1.MapGroup("/auth").WithTags("Auth"));
        ProdutosEndpoints.Map(v1.MapGroup("/produtos").WithTags("Produtos"));
        // Endpoints de estoque atrelados a produtos (/produtos/{id}/estoque)
        EstoqueEndpoints.MapProdutoEstoqueEndpoints(v1.MapGroup("/produtos").WithTags("Estoque"));
        // Endpoints de estoque globais (/estoque/baixo)
        EstoqueEndpoints.Map(v1.MapGroup("/estoque").WithTags("Estoque"));
        ImagensEndpoints.Map(v1.MapGroup("/imagens").WithTags("Imagens"));
        CategoriasEndpoints.Map(v1.MapGroup("/categorias").WithTags("Categorias"));
        ClientesEndpoints.Map(v1.MapGroup("/clientes").WithTags("Clientes"));
        EnderecosEndpoints.Map(v1.MapGroup("/enderecos").WithTags("Enderecos"));
        CarrinhoEndpoints.Map(v1.MapGroup("/carrinho").WithTags("Carrinho"));
        PedidosEndpoints.Map(v1.MapGroup("/pedidos").WithTags("Pedidos"));
        PagamentosEndpoints.Map(v1.MapGroup("/pagamentos").WithTags("Pagamentos"));
        RelatoriosEndpoints.Map(v1.MapGroup("/relatorios").WithTags("Relatorios"));
        RelatoriosEndpoints.MapRelatorioPorPeriodo(v1.MapGroup("/relatorios").WithTags("Relatorios"));
        RolesEndpoints.Map(v1.MapGroup("/roles").WithTags("Roles"));
        PermissionsEndpoints.Map(v1.MapGroup("/permissions").WithTags("Permissions"));
        UsuariosEndpoints.Map(v1.MapGroup("/usuarios").WithTags("Usuarios"));
    }
}
