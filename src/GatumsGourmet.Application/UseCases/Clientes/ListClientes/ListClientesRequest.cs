namespace GatumsGourmet.Application.UseCases.Clientes.ListClientes;

public record ListClientesRequest(int Page = 1, int PageSize = 10, string? Search = null);
public record ClienteResponse(Guid Id, string Nome, string Email, string? Telefone, string? Cidade, DateTime CriadoEm);
