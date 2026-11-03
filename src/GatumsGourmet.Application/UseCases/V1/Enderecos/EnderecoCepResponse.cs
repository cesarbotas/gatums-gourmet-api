namespace GatumsGourmet.Application.UseCases.V1.Enderecos;

/// <summary>
/// Endereço retornado pela consulta de CEP. Compatível com os campos de
/// <c>EnderecoResponse</c> do Cliente; <c>numero</c> não é fornecido pelo
/// provedor (fica a cargo do usuário no preenchimento).
/// </summary>
public record EnderecoCepResponse(
    string Cep,
    string Logradouro,
    string? Complemento,
    string Bairro,
    string Cidade,
    string Estado);
