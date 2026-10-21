namespace GatumsGourmet.Application.UseCases.Clientes.ListClientes;

public record ListClientesRequest(int Page = 1, int PageSize = 10, string? Search = null);

public record ClienteResponse(Guid Id,
    string Nome,
    string Email,
    string? Telefone,
    EnderecoResponse Endereco,
    DateTime CriadoEm);

public record EnderecoResponse(Guid Id,
    Guid ClienteId,
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Estado,
    string Cep);