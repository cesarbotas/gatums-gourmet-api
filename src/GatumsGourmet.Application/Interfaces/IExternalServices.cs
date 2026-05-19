using GatumsGourmet.Domain.Enums.Pagamentos;

namespace GatumsGourmet.Application.Interfaces;

public record CartaoInput(string Numero, string Nome, string Validade, string Cvv);
public record CartaoResult(bool Aprovado, string? Referencia, string? Ultimos4, string? Bandeira, string? Erro);
public record PixResult(string CopiaCola, DateTime ExpiraEm);

public interface IPaymentGateway
{
    Task<CartaoResult> ProcessarAsync(Guid transacaoId, decimal valor, MetodoPagamento metodo, CartaoInput cartao, CancellationToken ct = default);
}

public interface IPixProvider
{
    Task<PixResult> GerarAsync(Guid transacaoId, decimal valor, CancellationToken ct = default);
    Task<bool> ConfirmarAsync(Guid transacaoId, CancellationToken ct = default);
}

public interface IJwtTokenGenerator
{
    string Generate(Guid userId, string email, string role);
}
