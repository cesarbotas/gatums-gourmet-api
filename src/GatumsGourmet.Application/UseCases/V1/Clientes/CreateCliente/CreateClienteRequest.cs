namespace GatumsGourmet.Application.UseCases.V1.Clientes.CreateCliente;

public record CreateClienteRequest(
    string Nome,
    string Email,
    string? Telefone,
    EnderecoRequest Endereco
);

public record EnderecoRequest(
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Estado,
    string Cep
);