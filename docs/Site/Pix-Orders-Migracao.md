# Migração Pix → Mercado Pago Orders API

Referência para o time de front-end substituir o endpoint V1 de Pix pelo V2 (Orders API) em todos os fluxos.

## Por que migrar?

| | V1 `/api/v1/pagamentos/pix` | V2 `/api/v2/pagamentos/pix` |
|---|---|---|
| **API Mercado Pago** | v1/payments | v1/orders (mais atual) |
| **QR Code nativo** | ✅ | ✅ |
| **Webhook seguro** | Sem validação de assinatura | Valida `x-signature` |
| **Consulta de status** | Polling passivo | Sincroniza com MP ao consultar |
| **`payerEmail` obrigatório** | Não | Sim (exigido pela Orders API) |
| **Venda presencial** | ❌ usava V1 (errado) | ✅ corrigido para V2 |

---

## Resumo das mudanças de endpoint

| Fluxo | Antes | Depois |
|-------|-------|--------|
| Iniciar Pix (checkout online) | `POST /api/v1/pagamentos/pix` | `POST /api/v2/pagamentos/pix` |
| Consultar status Pix | `GET /api/v1/pagamentos/{id}/status` | `GET /api/v2/pagamentos/{id}/status` |
| Webhook Pix | `POST /api/v1/pagamentos/webhook/mercadopago` | `POST /api/v2/pagamentos/webhook/mercadopago` |
| Iniciar Pix presencial | `POST /api/v1/admin/vendas-presenciais/pagamento` | Mesmo endpoint, agora usa Orders internamente |
| Polling status presencial | `GET /api/v1/pagamentos/{id}/status` | `GET /api/v2/pagamentos/{id}/status` |

---

## 1. Fluxo online (checkout do site)

### Antes (V1)

```typescript
// src/lib/api/pagamentos.ts — ANTES
const response = await api.post('/api/v1/pagamentos/pix', {
  pedidoId,
  valor: pedido.total
});
```

### Depois (V2)

```typescript
// src/lib/api/pagamentos.ts — DEPOIS
const response = await api.post('/api/v2/pagamentos/pix', {
  pedidoId,
  payerEmail: usuario.email,  // OBRIGATÓRIO na V2
  payerNome: usuario.nome,    // opcional, melhora QR Code
  // valor: NÃO enviar — backend usa pedido.total
});
```

**Resposta 201 idêntica à V1** — nenhuma mudança no tratamento da resposta:

```typescript
interface PixResponse {
  id: string;               // transacaoId para polling
  status: 'pendente';
  metodo: 'pix';
  valor: number;
  pixCopiaCola: string;     // string copia-e-cola
  pixQrCodeBase64: string;  // imagem PNG em base64 → <img src={`data:image/png;base64,${...}`} />
  pixExpiraEm: string;      // ISO-8601
  operadora: 'mercadopago-orders';  // ← diferença: V1 devolvia 'mercadopago'
  operadoraRef: string;     // ID da order no Mercado Pago
}
```

### Consulta de status (polling)

```typescript
// ANTES
const status = await api.get(`/api/v1/pagamentos/${transacaoId}/status`);

// DEPOIS
const status = await api.get(`/api/v2/pagamentos/${transacaoId}/status`);
// ou pelo alias:
const status = await api.get(`/api/v2/pagamentos/pix/${transacaoId}/status`);
```

Comportamento novo: cada chamada ao V2 consulta o Mercado Pago e atualiza o status automaticamente se aprovado. O polling do front funciona igual — o backend que sincroniza.

```typescript
// Lógica de polling (não muda)
const polling = setInterval(async () => {
  const { status, pixExpiraEm } = await consultarStatus(transacaoId);

  if (status === 'aprovado') {
    clearInterval(polling);
    onAprovado();
    return;
  }

  if (new Date(pixExpiraEm) < new Date()) {
    clearInterval(polling);
    onExpirado();
  }
}, 4000);
```

---

## 2. Fluxo presencial (`/admin/venda-rapida`)

### O que mudou no backend (transparente para o front)

O endpoint `POST /api/v1/admin/vendas-presenciais/pagamento` com `metodo: 3` (Pix) agora chama internamente a **Orders API** em vez da v1/payments. O contrato do endpoint **não mudou**.

### O que o front precisa alterar

**Adicionar `payerEmail` no body** quando `metodo === 3` (Pix):

```typescript
// src/lib/api/venda-presencial.ts — ANTES
await api.post('/api/v1/admin/vendas-presenciais/pagamento', {
  pedidoId,
  metodo: 3,  // Pix
});

// DEPOIS — adicionar payerEmail
await api.post('/api/v1/admin/vendas-presenciais/pagamento', {
  pedidoId,
  metodo: 3,
  payerEmail: cliente.email ?? operador.email,  // obrigatório para Pix
});
```

> Se `payerEmail` não for informado, a API retorna:
> ```json
> { "status": 400, "detail": "E-mail do pagador é obrigatório para gerar o Pix. Informe 'payerEmail' no body." }
> ```

### Polling do status presencial → trocar para V2

```typescript
// ANTES — polling do status presencial
const { status } = await api.get(`/api/v1/pagamentos/${transacaoId}/status`);

// DEPOIS — usar V2 para sincronização automática
const { status } = await api.get(`/api/v2/pagamentos/${transacaoId}/status`);
```

---

## 3. Webhook

O Mercado Pago precisa ser configurado para apontar para o endpoint V2.

### URL do webhook (atualizar no painel do MP)

```
https://gatums-gourmet-api.onrender.com/api/v2/pagamentos/webhook/mercadopago
```

Variáveis de ambiente a configurar (Render ou VPS):

| Variável | Valor |
|----------|-------|
| `MercadoPago__AccessToken` | Token de produção/teste do MP |
| `MercadoPago__NotificationUrl` | `https://sua-api.com/api/v2/pagamentos/webhook/mercadopago` |
| `MercadoPago__WebhookSecret` | Chave de validação de assinatura (painel MP → Webhooks) |

> Sem `WebhookSecret` configurado, qualquer assinatura é aceita (útil para dev/testes).

---

## 4. Resumo dos arquivos a alterar no front

| Arquivo | Mudança |
|---------|---------|
| `src/lib/api/pagamentos.ts` | URL de `/api/v1/pagamentos/pix` → `/api/v2/pagamentos/pix` |
| `src/lib/api/pagamentos.ts` | Adicionar `payerEmail` obrigatório no body do Pix |
| `src/lib/api/pagamentos.ts` | URL de polling `/api/v1/…/status` → `/api/v2/…/status` |
| `src/lib/api/venda-presencial.ts` | Adicionar `payerEmail` ao body quando `metodo === 3` |
| `src/lib/api/venda-presencial.ts` | Polling de status presencial → `/api/v2/…/status` |

---

## 5. O que não muda

- Formato da resposta (todos os campos `pixCopiaCola`, `pixQrCodeBase64`, `pixExpiraEm` são iguais)
- Lógica de exibição do QR Code no front
- Endpoints de cartão (continuam na V1 e V2 iguais)
- Endpoint de criação do pedido presencial (`POST /api/v1/admin/vendas-presenciais`)
- Endpoints de confirmar cartão na maquininha
