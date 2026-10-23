using GatumsGourmet.Api.Endpoints;

namespace GatumsGourmet.Api.Extensions;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapGatumsEndpoints(this IEndpointRouteBuilder app)
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

        return app;
    }
}
