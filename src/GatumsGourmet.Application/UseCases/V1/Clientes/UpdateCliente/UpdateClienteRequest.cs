namespace GatumsGourmet.Application.UseCases.V1.Clientes.UpdateCliente;

public record UpdateClienteRequest(
    Guid Id,
    string Nome,
    string Email,
    string? Telefone
);