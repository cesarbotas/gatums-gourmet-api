namespace GatumsGourmet.Domain.Exceptions;

/// <summary>
/// Falha na integração com o provider de Pix via Orders API (resposta de erro ou payload inválido).
/// </summary>
public sealed class PixOrdersProviderException(string message, int? statusCode = null) : Exception(message)
{
    public int? StatusCode { get; } = statusCode;
}