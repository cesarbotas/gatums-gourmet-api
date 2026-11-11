# Guia de Integração: Pagamento PIX via Mercado Pago

Este documento é o guia oficial e passo a passo para configurar e operar a integração de pagamentos **PIX do Mercado Pago** na **Gatums Gourmet API** (.NET 10).

---

## 1. Visão Geral da Arquitetura

A integração utiliza a API REST v1 oficial do Mercado Pago (`https://api.mercadopago.com/v1/payments`) com comunicação direta via `HttpClientFactory` fortemente tipada e resiliente, sem dependências externas obsoletas.

### Fluxo de Pagamento Ponta a Ponta:

```
[Cliente no App/Site]
         │
         ▼ 1. POST /api/v1/pagamentos/pix { pedidoId }
[Gatums Gourmet API]
         │
         ▼ 2. POST https://api.mercadopago.com/v1/payments (com AccessToken)
[Mercado Pago] ────► Gera QR Code Copia e Cola + Imagem Base64 + ID do Pagamento
         │
         ▼ 3. Retorna { pixCopiaCola, pixQrCodeBase64, pixExpiraEm, operadoraRef }
[Cliente visualiza QR Code e Copia-e-Cola]
         │
         ▼ 4. Cliente paga no app do seu banco
[Banco / BACEN]
         │
         ▼ 5. Mercado Pago recebe a compensação instantânea
[Mercado Pago Webhook]
         │
         ▼ 6. POST /api/v1/pagamentos/webhook/mercadopago
[Gatums Gourmet API] ────► Valida autenticidade no MP e atualiza Pedido para APROVADO!
```

---

## 2. Passo a Passo no Painel do Mercado Pago

### Passo 2.1 — Acessar o Portal de Desenvolvedores
1. Faça login na sua conta do Mercado Pago onde você deseja receber as vendas.
2. Acesse o **Portal de Desenvolvedores**: [https://www.mercadopago.com.br/developers](https://www.mercadopago.com.br/developers).
3. No menu superior, clique em **Suas integrações** (ou acesse [https://www.mercadopago.com.br/developers/panel/app](https://www.mercadopago.com.br/developers/panel/app)).

### Passo 2.2 — Criar a Aplicação
1. Clique no botão **Criar aplicação**.
2. Preencha os campos:
   * **Nome da aplicação:** `Gatums Gourmet API` (ou nome da sua hamburgueria).
   * **Tipo de solução de pagamento:** Selecione **Pagamentos online** (Checkout Transparente / API).
   * **Você está usando uma plataforma de e-commerce?** Selecione **Não**.
   * **Qual produto você está integrando?** Selecione **Checkout Transparente** (ou Pagamento via API).
3. Aceite os Termos e Condições e clique em **Criar aplicação**.

### Passo 2.3 — Obter as Credenciais (Access Token)
1. Dentro do painel da sua aplicação recém-criada, no menu lateral esquerdo, clique em **Credenciais**.
2. Você verá duas abas:
   * **Credenciais de teste (Sandbox):**
     * O `Access Token` começa com `TEST-...`.
     * Use para testar localmente ou em homologação sem movimentar dinheiro real.
   * **Credenciais de produção:**
     * O `Access Token` começa com `APP_USR-...`.
     * Use quando a API estiver no ar em Produção (VPS Locaweb).
3. **Copie o `Access Token`** da aba desejada. Este token será configurado na API.

> [!IMPORTANT]
> **Cadastre uma chave PIX na sua conta do Mercado Pago!**  
> Para poder gerar cobranças PIX, a sua conta Mercado Pago deve possuir ao menos uma chave PIX cadastrada e validada (CPF, CNPJ, e-mail, telefone ou chave aleatória). Se não houver chave cadastrada, a API do Mercado Pago rejeitará as requisições com erro de validação.

---

## 3. Configuração na Gatums Gourmet API

### Variáveis de Ambiente Suportadas

A API busca as configurações na seção `MercadoPago`:

| Variável de Ambiente | Chave no appsettings.json | Descrição | Exemplo |
|---|---|---|---|
| `MercadoPago__AccessToken` | `MercadoPago:AccessToken` | Token privado de acesso da sua aplicação | `TEST-123456...` ou `APP_USR-123456...` |
| `MercadoPago__NotificationUrl` | `MercadoPago:NotificationUrl` | URL pública onde o Mercado Pago enviará os webhooks | `https://api.gatumsgourmet.com.br/api/v1/pagamentos/webhook/mercadopago` |
| `MercadoPago__WebhookSecret` | `MercadoPago:WebhookSecret` | (Opcional) Segredo HMAC para assinatura do webhook | `sec_xxxx...` |
| `MercadoPago__BaseUrl` | `MercadoPago:BaseUrl` | (Opcional) Base URL da API do MP (default já é oficial) | `https://api.mercadopago.com/` |

> [!NOTE]
> **Modo Fallback Automático:**  
> Se `MercadoPago:AccessToken` estiver em branco (ou não configurado), a API roda automaticamente com o `MockPixProvider` integrado. Isso garante que outros desenvolvedores possam rodar a suíte de testes e o projeto sem precisar de uma conta do Mercado Pago.

### Configurando no arquivo `.env` (Local / Docker / VPS):
```bash
MercadoPago__AccessToken=APP_USR-1234567890123456-abcdef...
MercadoPago__NotificationUrl=https://api.seusite.com.br/api/v1/pagamentos/webhook/mercadopago
```

### Configurando no `appsettings.json`:
```json
{
  "MercadoPago": {
    "BaseUrl": "https://api.mercadopago.com/",
    "AccessToken": "SEU_ACCESS_TOKEN_AQUI",
    "NotificationUrl": "https://api.seusite.com.br/api/v1/pagamentos/webhook/mercadopago"
  }
}
```

---

## 4. Configuração do Webhook no Mercado Pago

Para que os pedidos sejam aprovados automaticamente assim que o cliente pagar no banco:

1. No painel de desenvolvedores do Mercado Pago, entre na sua aplicação e clique em **Webhooks** (ou **Notificações IPN**).
2. Configure o modo **Modo Produção** ou **Modo Teste** (conforme o ambiente).
3. Em **URL de produção**, insira a URL pública da sua API:
   ```
   https://api.seudominio.com.br/api/v1/pagamentos/webhook/mercadopago
   ```
4. Em **Eventos**, selecione o evento:
   * ✅ **Pagamentos** (`payment`)
5. Clique em **Salvar**.

### Como testar Webhooks em Desenvolvimento Local (ngrok)
Se você estiver rodando a API localmente na porta `5166` e quiser receber webhooks reais do Mercado Pago:
1. Instale o ngrok (`brew install ngrok` ou baixe em ngrok.com).
2. Execute no terminal:
   ```bash
   ngrok http 5166
   ```
3. O ngrok gerará uma URL HTTPS pública, por exemplo: `https://abc123.ngrok-free.app`.
4. Configure a URL no Mercado Pago:
   ```
   https://abc123.ngrok-free.app/api/v1/pagamentos/webhook/mercadopago
   ```

---

## 5. Novos Endpoints Disponíveis

### 1. Iniciar Pagamento PIX
* **Método e Rota:** `POST /api/v1/pagamentos/pix`
* **Autenticação:** Exige Bearer Token (`clienteToken` ou `adminToken`)
* **Headers:** `Idempotency-Key` (opcional, para evitar cobranças duplicadas)
* **Request Body:**
  ```json
  {
    "pedidoId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "valor": 65.80,              // opcional: se omitido ou 0, assume o Total do pedido
    "payerEmail": "cliente@email.com", // opcional: se omitido, usa o do cadastro
    "payerNome": "João da Silva"  // opcional: se omitido, usa o do pedido
  }
  ```
* **Response 201 Created:**
  ```json
  {
    "id": "7b89d421-4d1e-4c7a-9a99-b1d8f58b0901",
    "pedidoId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "metodo": "pix",
    "valor": 65.80,
    "status": "pendente",
    "pixCopiaCola": "00020126580014BR.GOV.BCB.PIX0136...",
    "pixQrCodeBase64": "iVBORw0KGgoAAAANSUhEUgAA...",
    "pixExpiraEm": "2026-11-11T02:00:00Z",
    "operadora": "mercadopago",
    "operadoraRef": "12345678901",
    "cartaoUltimos4": null,
    "cartaoBandeira": null,
    "mensagemErro": null,
    "criadoEm": "2026-11-10T23:00:00Z",
    "atualizadoEm": "2026-11-10T23:00:00Z"
  }
  ```

> [!TIP]
> **Renderização no Front-end:**  
> - Para exibir o QR Code em imagem diretamente:
>   ```html
>   <img src="data:image/png;base64,{pixQrCodeBase64}" alt="QR Code PIX" />
>   ```
> - Para o botão "Copiar Chave Pix": copie o conteúdo do campo `pixCopiaCola`.

---

### 2. Consultar Status do PIX (Auto-Sync)
* **Método e Rota:** `GET /api/v1/pagamentos/pix/{transacaoId}/status`
* **Autenticação:** Bearer Token
* **Descrição:** Retorna a transação. Se estiver pendente e possuir vínculo com o Mercado Pago, a API consulta o Mercado Pago em tempo real. Se o status no MP já for `approved`, a API atualiza imediatamente a transação e o pedido no banco de dados!
* **Response 200 OK:**
  ```json
  {
    "id": "7b89d421-4d1e-4c7a-9a99-b1d8f58b0901",
    "pedidoId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "metodo": "pix",
    "valor": 65.80,
    "status": "aprovado",
    "operadora": "mercadopago",
    "operadoraRef": "12345678901"
  }
  ```

---

### 3. Webhook de Confirmação Automática (Mercado Pago)
* **Método e Rota:** `POST /api/v1/pagamentos/webhook/mercadopago`
* **Autenticação:** Aberto / Público (`AllowAnonymous`)
* **Descrição:** Recebe notificações do Mercado Pago via querystring (`?id=...` ou `?data.id=...`) ou corpo JSON (`{ "data": { "id": "..." } }`). Valida o pagamento no Mercado Pago e atualiza o pedido e a transação para `aprovado`.
* **Response 200 OK:**
  ```json
  true
  ```

---

## 6. Endpoints Especializados para Cartões

Além do PIX, os pagamentos foram separados e ganharam endpoints dedicados:

### Cartão de Crédito
* **`POST /api/v1/pagamentos/cartao-credito`**
* **Body:**
  ```json
  {
    "pedidoId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "valor": 65.80,
    "numero": "4111111111111111",
    "nome": "JOAO DA SILVA",
    "validade": "12/2028",
    "cvv": "123",
    "parcelas": 1
  }
  ```

### Cartão de Débito
* **`POST /api/v1/pagamentos/cartao-debito`**
* **Body:**
  ```json
  {
    "pedidoId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "valor": 65.80,
    "numero": "4111111111111111",
    "nome": "JOAO DA SILVA",
    "validade": "12/2028",
    "cvv": "123"
  }
  ```

---

## 7. Resolução de Problemas Comuns (Troubleshooting)

1. **Erro `MercadoPago:AccessToken não está configurado`:**
   * Certifique-se de que definiu a variável `MercadoPago__AccessToken` no seu arquivo `.env` ou nas variáveis do container / provedor de nuvem (Locaweb / Render).
2. **Erro `payer.email is required`:**
   * O Mercado Pago exige que o cliente tenha um e-mail válido. Se não informar no body `payerEmail`, a API usa automaticamente o e-mail cadastrado no usuário do pedido ou o fallback `contato@gatumsgourmet.com.br`.
3. **Webhook não atualiza o pedido:**
   * Certifique-se de que a URL configurada no painel do Mercado Pago é pública e acessível via HTTPS.
   * Se estiver em localhost, use o `ngrok` ou consulte diretamente o endpoint `GET /api/v1/pagamentos/pix/{transacaoId}/status`, que possui sincronização automática em tempo real.

