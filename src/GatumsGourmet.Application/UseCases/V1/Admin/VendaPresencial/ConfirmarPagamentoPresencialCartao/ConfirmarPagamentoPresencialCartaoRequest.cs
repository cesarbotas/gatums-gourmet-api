namespace GatumsGourmet.Application.UseCases.V1.Admin.VendaPresencial.ConfirmarPagamentoPresencialCartao;

/// <param name="Aprovado">true = aprovado na maquininha; false = recusado.</param>
/// <param name="Observacao">Ex.: "Aprovado na maquininha Stone #0042".</param>
public record ConfirmarPagamentoPresencialCartaoRequest(bool Aprovado, string? Observacao = null);
