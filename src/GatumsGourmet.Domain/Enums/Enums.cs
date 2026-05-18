namespace GatumsGourmet.Domain.Enums;

public enum UserRole { Admin, Cliente }
public enum TipoProduto { Simples, Combo }
public enum PedidoEtapa { Criado, Aprovado, Pagamento, PagamentoAprovado, EnvioPendente, Entregue, Finalizado }
public enum PedidoActor { Cliente, Admin }
public enum EntregaTipo { Transporte, Retirada }
public enum MetodoPagamento { CartaoDebito, CartaoCredito, Pix }
public enum StatusTransacao { Pendente, Aprovado, Recusado, Expirado }
