# Busca de endereço por CEP

Endpoint público que consulta um CEP e devolve o endereço, para **autopreencher**
o formulário de endereço no front. Read-only — não grava nem vincula nada a um
cliente. Fonte: [ViaCEP](https://viacep.com.br).

---

## Endpoint

```
GET /api/v1/enderecos/cep/{cep}
```

- **Autenticação:** nenhuma (público, como o catálogo de produtos).
- **`{cep}`:** 8 dígitos. Aceita com ou sem máscara — `01001000` ou `01001-000`
  (a API normaliza removendo tudo que não for dígito).

### Resposta 200 (sucesso)

```json
{
  "cep": "01001000",
  "logradouro": "Praça da Sé",
  "complemento": "lado ímpar",
  "bairro": "Sé",
  "cidade": "São Paulo",
  "estado": "SP"
}
```

- `complemento` pode vir `null`.
- **`numero` não é retornado** — o ViaCEP não fornece; o usuário preenche no formulário.
- Os campos batem com o `EnderecoResponse` do Cliente (exceto `numero`/`id`/`clienteId`),
  então dá para mapear direto para o formulário de endereço.

### Erros (HTTP 400)

Todos os erros de negócio retornam **400** no formato ProblemDetails:

```json
{ "detail": "CEP não encontrado.", "status": 400 }
```

| `detail` | Quando |
|----------|--------|
| `CEP inválido.` | Formato incorreto (não tem 8 dígitos após remover a máscara) |
| `CEP não encontrado.` | CEP bem formado, mas inexistente na base do ViaCEP |
| `Serviço de CEP indisponível.` | ViaCEP fora do ar ou timeout (5s) |

> O front deve tratar qualquer 400 aqui como "não foi possível preencher pelo CEP" e
> deixar o usuário digitar o endereço manualmente — nunca bloquear o cadastro por isso.

---

## Consumo no front (React + TanStack)

Usa o mesmo `apiFetch` do guia de autenticação
([`Migracao-Front-Auth-RBAC.md`](Migracao-Front-Auth-RBAC.md)), mas **sem Bearer** (é público —
`apiFetch` só anexa o token se houver, então funciona anônimo também).

```ts
// src/lib/api/cep.ts
import { apiFetch } from "./http";

export interface EnderecoCep {
  cep: string;
  logradouro: string;
  complemento: string | null;
  bairro: string;
  cidade: string;
  estado: string;
}

/** Retorna o endereço do CEP, ou null se inválido/não encontrado/indisponível. */
export async function buscarCep(cep: string): Promise<EnderecoCep | null> {
  const limpo = cep.replace(/\D/g, "");
  if (limpo.length !== 8) return null; // evita uma ida à API para CEP incompleto

  const resp = await apiFetch(`/enderecos/cep/${limpo}`);
  if (!resp.ok) return null; // 400 → deixa o usuário preencher à mão
  return resp.json();
}
```

### Com TanStack Query (busca ao completar o CEP)

```ts
import { useQuery } from "@tanstack/react-query";
import { buscarCep } from "@/lib/api/cep";

export function useCep(cep: string) {
  const limpo = cep.replace(/\D/g, "");
  return useQuery({
    queryKey: ["cep", limpo],
    queryFn: () => buscarCep(limpo),
    enabled: limpo.length === 8, // só dispara quando o CEP está completo
    staleTime: 1000 * 60 * 60,   // CEP não muda; cacheia por 1h
  });
}
```

### Autopreenchimento do formulário

```tsx
const { data: endereco, isFetching } = useCep(cepField);

useEffect(() => {
  if (endereco) {
    form.setValue("logradouro", endereco.logradouro);
    form.setValue("bairro", endereco.bairro);
    form.setValue("cidade", endereco.cidade);
    form.setValue("estado", endereco.estado);
    form.setValue("complemento", endereco.complemento ?? "");
    // numero fica para o usuário preencher
    numeroInputRef.current?.focus();
  }
}, [endereco]);
```

---

## Notas

- **Rate limit:** o ViaCEP é gratuito e sem chave, mas não é ilimitado. O `enabled` do
  TanStack (só dispara com 8 dígitos) + o `staleTime` de 1h já evitam chamadas desnecessárias.
  Se o volume crescer, dá para adicionar cache no backend (hoje não há — YAGNI).
- **Configuração (backend):** a base URL do ViaCEP fica em `appsettings.json`
  (`"ViaCep": { "BaseUrl": "https://viacep.com.br/ws/" }`), então dá para trocar de
  provedor (ex.: BrasilAPI) sem mexer no código — só na config e na implementação do
  `ICepProvider`.
- **Referência da API:** ver também [`API-Reference.md`](API-Reference.md).
