namespace GatumsGourmet.Domain.Enums.Pedidos;

/// <summary>
/// Tipo de entrega registrado na etapa <see cref="StatusPedido.Entregue"/> da timeline.
/// Armazenado como <see langword="string"/> no banco de dados.
/// </summary>
public enum TipoEntrega
{
    /// <summary>1 — Entrega via transportadora ou motoboy.</summary>
    Transporte = 1,

    /// <summary>2 — Cliente retirou pessoalmente no balcão/loja.</summary>
    Retirada = 2
}
