# Upload de imagens de produto

Endpoint separado que recebe o arquivo da imagem, valida, grava num bucket
S3-compatível e devolve a **URL pública**. O cadastro de produto continua 100%
JSON: o front faz o upload primeiro e depois grava **apenas a URL** em `fotoUrl`
no POST/PUT de produto. A imagem nunca trafega dentro do corpo do produto.

---

## Endpoint

```
POST /api/v1/imagens
```

- **Autenticação:** Bearer **com a permissão `produtos:escrever`** (a mesma do
  CRUD de produtos). Anônimo → `401`; autenticado sem a permissão → `403`.
- **Content-Type:** `multipart/form-data`.
- **Campo:** `arquivo` (um único arquivo).
- **Tipos aceitos:** JPEG, PNG e WebP. O tipo é detectado pelos **magic bytes** do
  conteúdo — o `Content-Type` enviado e a extensão do nome **não** decidem nada
  (renomear um `.txt` para `.jpg` não engana a validação).
- **Tamanho máximo:** 5 MB (configurável em `Storage:MaxFileSizeMB`).

### Resposta 201 (sucesso)

```json
{ "url": "https://.../produtos/5f3c0a1b8e4d4f7e9c2a1b0d6e8f3a2c.jpg" }
```

- O corpo tem **apenas** `url` — a URL pública completa do objeto.
- A API gera o nome do objeto como `produtos/{guid}.{ext}`; o cliente não escolhe
  o nome nem o caminho.
- Use essa `url` como `fotoUrl` ao criar/atualizar o produto.

### Erros

Os erros de negócio retornam **ProblemDetails**:

```json
{ "detail": "Tipo de imagem não suportado. Use JPEG, PNG ou WebP.", "status": 400 }
```

| Status | `detail` / situação | Quando |
|--------|---------------------|--------|
| `400` | `Tipo de imagem não suportado. Use JPEG, PNG ou WebP.` | O conteúdo não é JPEG/PNG/WebP (magic bytes) |
| `400` | `Arquivo vazio.` | Nenhum arquivo enviado ou arquivo com 0 byte |
| `400` | `Arquivo excede o tamanho máximo de 5 MB.` | Acima do limite configurado |
| `401` | sem corpo | Requisição sem token |
| `403` | sem corpo | Token sem a permissão `produtos:escrever` |
| `500` | mensagem clara de configuração | Storage não configurado no ambiente |

> O `400` cobre todo erro de validação do arquivo. O `500` só aparece se o
> ambiente não tiver a seção `Storage` configurada (ver [Notas](#notas)).

---

## Consumo no front (React + TanStack)

Usa o mesmo `apiFetch` do guia de autenticação
([`Migracao-Front-Auth-RBAC.md`](Migracao-Front-Auth-RBAC.md)) — **com Bearer**,
porque o upload exige a permissão `produtos:escrever`. Com `FormData`, **não
defina o `Content-Type` manualmente**: o browser injeta o `boundary` do
multipart sozinho.

```ts
// src/lib/api/imagens.ts
import { apiFetch } from "./http";

/** Faz upload de uma imagem de produto e devolve a URL pública. */
export async function uploadImagem(file: File): Promise<string> {
  const form = new FormData();
  form.append("arquivo", file); // o campo PRECISA se chamar "arquivo"

  // não passe Content-Type: o browser monta o multipart/form-data + boundary
  const resp = await apiFetch("/imagens", { method: "POST", body: form });
  if (!resp.ok) {
    const problem = await resp.json().catch(() => null);
    throw new Error(problem?.detail ?? "Falha ao enviar a imagem.");
  }

  const { url } = await resp.json();
  return url; // use como fotoUrl no produto
}
```

### Encadeamento: upload → URL → produto

O upload é um passo separado. Primeiro envia o arquivo, recebe a `url`, e só
então grava a string no produto:

```ts
import { uploadImagem } from "@/lib/api/imagens";
import { apiFetch } from "@/lib/api/http";

async function salvarProdutoComImagem(form: ProdutoForm, file?: File) {
  // 1. se o usuário escolheu um arquivo, sobe a imagem primeiro
  const fotoUrl = file ? await uploadImagem(file) : form.fotoUrl;

  // 2. grava o produto com APENAS a URL (o produto é 100% JSON)
  const resp = await apiFetch("/produtos", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ ...form, fotoUrl }),
  });
  return resp.json();
}
```

### Com TanStack Query (mutation)

```ts
import { useMutation } from "@tanstack/react-query";
import { uploadImagem } from "@/lib/api/imagens";

export function useUploadImagem() {
  return useMutation({
    mutationFn: (file: File) => uploadImagem(file),
    // onSuccess: (url) => form.setValue("fotoUrl", url)
  });
}
```

> Trocar a foto de um produto = novo upload + atualizar o `fotoUrl`. A imagem
> antiga permanece no bucket (limpeza de órfãs está fora de escopo por ora).

---

## Notas

- **Storage por ambiente:** o backend grava num storage S3-compatível escolhido
  por ambiente (seção `Storage` do appsettings):

  | Ambiente | Backend | Observação |
  |----------|---------|------------|
  | Local / dev | **MinIO** (docker-compose) | `http://localhost:9000`, bucket `gatums-imagens`, console em `http://localhost:9001` (`minioadmin`/`minioadmin`) |
  | Homologação | **Supabase Storage** | tier grátis; endpoint S3 |
  | Produção | **Cloudflare R2** | S3-compatível, egress gratuito |

  As chaves (`AccessKey`/`SecretKey`) vêm por variável de ambiente
  (`Storage__AccessKey` / `Storage__SecretKey`) fora do local; o MinIO local usa
  credenciais fixas sem valor de segurança.
- **Leitura pública:** as URLs retornadas são de leitura pública (os buckets
  ficam em modo leitura-pública), então o front pode usar a `url` direto em
  `<img src=...>` sem token.
- **Tipos e tamanho configuráveis:** o limite de negócio de 5 MB está em
  `Storage:MaxFileSizeMB` (retorna `400` com mensagem clara acima dele). Além
  disso, o endpoint tem um teto de corpo de ~6 MB no nível do framework —
  requisições grosseiramente grandes são barradas antes de chegar ao handler
  (`413`). Os tipos aceitos são JPEG/PNG/WebP (validados por magic bytes).
- **Subir o MinIO local:** `docker compose -f docker/docker-compose.yml up -d`
  sobe também o MinIO com o bucket `gatums-imagens` já público. Dá para conferir
  os uploads visualmente no console em `http://localhost:9001`.
- **Nota técnica (MinIO sobre HTTP):** o cliente S3 usa
  `RequestChecksumCalculation = WHEN_REQUIRED` para funcionar com o MinIO em
  `http://` local (detalhe de implementação; transparente para o front).
- **Referência da API:** ver também [`API-Reference.md`](API-Reference.md#imagens).
