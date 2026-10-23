namespace GatumsGourmet.Domain.Entities.Pagamentos;

public record CartaoResult(bool Aprovado, string? Referencia, string? Ultimos4, string? Bandeira, string? Erro);
