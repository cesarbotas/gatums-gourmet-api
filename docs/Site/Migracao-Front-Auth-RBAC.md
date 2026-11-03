# Migração do Front — Autenticação RBAC + Refresh Token

Guia para adaptar o site **gatums-gourmet-hub** (React + TanStack + Vite) ao novo
esquema de autenticação da API. Contexto completo em
[`docs/Auth/Guia-de-Uso.md`](../Auth/Guia-de-Uso.md) e
[`docs/Site/API-Reference.md`](API-Reference.md).

> **Por que mudou:** o login agora devolve um par **access token (15 min)** +
> **refresh token (7 dias)**, em vez de um token único de 8h. A autorização passou
> a ser por **permissão**, e `user.role` (string) virou `user.roles` (array).

---

## 1. Mudanças de contrato (resumo)

| O que | Antes | Agora |
|------|-------|-------|
| Login response | `{ token, user: { role } }` | `{ accessToken, refreshToken, expiresIn, user: { id, nome, email, roles: [] } }` |
| Campo do token | `token` | `accessToken` |
| Papel do usuário | `user.role` (string) | `user.roles` (array de string) |
| Renovação | não existia | `POST /auth/refresh` com `{ refreshToken }` |
| Logout | no-op | `POST /auth/logout` com `{ refreshToken }` (revoga sessão) |
| Encerrar tudo | não existia | `POST /auth/logout-all` |
| Access token | 8h | 15 min (renovar via refresh) |

Endpoints de auth (base `VITE_API_BASE_URL`, ex. `/api/v1`):
- `POST /auth/login` → `{ email, senha }`
- `POST /auth/register` → `{ nome, email, senha, telefone? }` (cria Cliente)
- `POST /auth/refresh` → `{ refreshToken }` → mesmo shape do login (rotação: o refresh antigo é invalidado)
- `POST /auth/logout` (Bearer) → `{ refreshToken }`
- `POST /auth/logout-all` (Bearer)
- `GET /auth/me` (Bearer) → `{ id, nome, email, roles: [] }`

---

## 2. Armazenamento dos tokens

Guarde os dois tokens. Recomendação pragmática para SPA desacoplado:
- `accessToken` em memória (ou `localStorage` se precisar sobreviver a refresh de página).
- `refreshToken` em `localStorage`.

> Observação de segurança: `localStorage` é vulnerável a XSS. A alternativa mais
> segura seria o refresh em cookie `HttpOnly`, mas isso exigiria mudança de CORS/credenciais
> na API (está fora do escopo desta fase — ver `docs/Auth/Guia-de-Uso.md`). Para o uso atual
> (admin/testes), `localStorage` é aceitável; registre como dívida se expor a clientes.

```ts
// src/lib/auth/tokenStore.ts
const ACCESS = "gg.accessToken";
const REFRESH = "gg.refreshToken";

export const tokenStore = {
  get access() { return localStorage.getItem(ACCESS); },
  get refresh() { return localStorage.getItem(REFRESH); },
  set(tokens: { accessToken: string; refreshToken: string }) {
    localStorage.setItem(ACCESS, tokens.accessToken);
    localStorage.setItem(REFRESH, tokens.refreshToken);
  },
  clear() {
    localStorage.removeItem(ACCESS);
    localStorage.removeItem(REFRESH);
  },
};
```

---

## 3. Cliente HTTP com refresh automático (fetch)

Interceptor que injeta o Bearer, e em caso de **401** tenta renovar uma vez e repete a
requisição. Trata concorrência (vários 401 simultâneos compartilham um único refresh em voo).

```ts
// src/lib/api/http.ts
import { tokenStore } from "../auth/tokenStore";

const BASE = import.meta.env.VITE_API_BASE_URL ?? "/api/v1";

let refreshing: Promise<boolean> | null = null;

async function doRefresh(): Promise<boolean> {
  const refreshToken = tokenStore.refresh;
  if (!refreshToken) return false;

  const resp = await fetch(`${BASE}/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken }),
  });

  if (!resp.ok) {
    tokenStore.clear();
    return false;
  }
  const data = await resp.json();
  tokenStore.set({ accessToken: data.accessToken, refreshToken: data.refreshToken });
  return true;
}

export async function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
  const withAuth = (): RequestInit => {
    const headers = new Headers(init.headers);
    const token = tokenStore.access;
    if (token) headers.set("Authorization", `Bearer ${token}`);
    if (init.body && !headers.has("Content-Type")) headers.set("Content-Type", "application/json");
    return { ...init, headers };
  };

  let resp = await fetch(`${BASE}${path}`, withAuth());

  if (resp.status === 401 && tokenStore.refresh) {
    // Compartilha um único refresh entre chamadas concorrentes.
    refreshing ??= doRefresh().finally(() => { refreshing = null; });
    const ok = await refreshing;
    if (ok) {
      resp = await fetch(`${BASE}${path}`, withAuth()); // repete com o novo access
    } else {
      // refresh falhou → sessão encerrada; redirecione para login
      redirectToLogin();
    }
  }

  return resp;
}

function redirectToLogin() {
  tokenStore.clear();
  if (typeof window !== "undefined") window.location.assign("/login");
}
```

> Se o front usa **axios**, a mesma ideia vira um `axios.interceptors.response` que,
> no 401, chama `doRefresh()` e refaz a requisição original (`error.config`). Avise se
> quiser a versão axios.

---

## 4. Login / logout (TanStack Query)

```ts
// src/lib/api/auth.ts
import { apiFetch } from "./http";
import { tokenStore } from "../auth/tokenStore";

export async function login(email: string, senha: string) {
  const resp = await apiFetch("/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, senha }),
  });
  if (!resp.ok) throw new Error("Credenciais inválidas.");
  const data = await resp.json(); // { accessToken, refreshToken, expiresIn, user }
  tokenStore.set({ accessToken: data.accessToken, refreshToken: data.refreshToken });
  return data.user; // { id, nome, email, roles: [] }
}

export async function logout() {
  const refreshToken = tokenStore.refresh;
  if (refreshToken) {
    await apiFetch("/auth/logout", { method: "POST", body: JSON.stringify({ refreshToken }) });
  }
  tokenStore.clear();
}

export async function me() {
  const resp = await apiFetch("/auth/me");
  if (!resp.ok) return null;
  return resp.json(); // { id, nome, email, roles: [] }
}
```

```ts
// Exemplo de mutation de login com TanStack Query
import { useMutation } from "@tanstack/react-query";
import { login } from "@/lib/api/auth";

export const useLogin = () =>
  useMutation({ mutationFn: ({ email, senha }: { email: string; senha: string }) => login(email, senha) });
```

---

## 5. Controle de UI por papel/permissão

`user.role` (string) **não existe mais**. Use `user.roles` (array). Se mostrava/escondia
algo com `user.role === "admin"`:

```ts
// Antes
const isAdmin = user.role === "admin";

// Agora
const isAdmin = user.roles?.includes("Admin");
```

> Atenção ao casing: os nomes de role são **PascalCase** (`"Admin"`, `"Cliente"`, `"Gerente"`),
> não minúsculo.

Se quiser granularidade fina (ex.: mostrar o botão "Novo produto" só para quem pode criar
produto), o front hoje recebe só `roles` no `/me`. As permissões vivem no **access token**
(claims `perm`). Duas opções:
- **Simples:** basear a UI em `roles` (ex.: `Admin`/`Gerente` veem a gestão).
- **Fino:** decodificar o JWT no front e ler as claims `perm` (ex.: `jwt-decode`), ou pedir
  um endpoint `/auth/me` estendido que retorne as permissões (não existe hoje — avise se quiser).

---

## 6. Checklist de migração do front

- [ ] Trocar leitura de `token` → `accessToken` na resposta de login.
- [ ] Guardar e usar o `refreshToken`.
- [ ] Implementar o interceptor 401 → `/auth/refresh` → repetir (seção 3).
- [ ] Trocar `user.role` (string) por `user.roles` (array) em toda a UI; atenção ao PascalCase.
- [ ] Logout passa a chamar `POST /auth/logout` com `{ refreshToken }`.
- [ ] (Opcional) tela de "encerrar todas as sessões" via `/auth/logout-all`.
- [ ] (Opcional) backoffice de RBAC via `/roles`, `/permissions`, `/usuarios`.
- [ ] Após o deploy desta versão, **relogar** (tokens antigos não valem mais).

---

## 7. CORS (nota)

A configuração de CORS por ambiente ficou como **Fase 2** (a API hoje libera as origens de
forma ampla). Se o front rodar em um domínio novo (ex.: preview da Vercel) e houver bloqueio
de CORS, é sinal de que a Fase 2 precisa ser priorizada. Avise se topar isso.
