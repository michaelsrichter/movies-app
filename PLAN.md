# Implementation Plan — Family Movie Watchlist

A mobile-first family movie watchlist hosted on **Azure Static Web Apps (Free tier)**, eventually at
**https://movies.mikerichter.app**. Public users browse a curated list ("Summer 2026") with rich
TMDB-backed detail pages and **AI-authored family discussion topics**. An admin section (GitHub auth)
manages lists and content.

> **Status:** Phase 0 scaffold in progress. Read the **Resolved constraints** section first — two of the
> brief's "hard requirements" are not supported by the chosen platform and were resolved with documented
> decisions.

---

## Resolved constraints (deviations from the brief — flagged)

During planning, two hard requirements were found to be **impossible on Azure Static Web Apps Free tier
with managed Functions**. Both are documented here and approved.

### 1. Managed Identity → resolved as "Fork A" (connection string)
Azure SWA **managed Functions do not support Managed Identity** (or Key Vault references). Managed Identity
is only available with **Bring Your Own Functions**, which requires the **Standard plan** and a separate
Function App resource — contradicting "Free tier", "managed API", and "no standalone Function App".

**Decision (Fork A):** Stay on Free + managed Functions. In Azure, Storage and Azure OpenAI authenticate
with **keys/connection strings stored in SWA application settings** (encrypted at rest, never committed).
For **local development**, the code prefers `DefaultAzureCredential` (your `az login`) so account keys never
sit on the dev machine. A `StorageClientFactory` chooses automatically:

- If `Storage__ConnectionString` is set → use it (Azure path).
- Else if `Storage__AccountName` is set → use `DefaultAzureCredential` against the account endpoint (local path).

**Residual risk:** the Storage account key lives in the SWA app settings in Azure. Upgrade path to true MI =
move to Standard plan + Bring Your Own Functions later (no data migration needed).

### 2. .NET 10 isolated → resolved as **.NET 9 isolated**
Managed Functions support a maximum of **`dotnet-isolated:9.0`**. **.NET 10 is not available** on managed
Functions. We target **.NET 9 isolated**. (.NET 10 would only be possible via Bring Your Own Functions.)

### 3. Other platform notes (non-blocking)
- **API request timeout is 45 seconds** → bulk TMDB refresh is **chunked** to stay under the limit.
- **Free tier allows 2 custom domains per app** → fine for `movies.mikerichter.app` (+ optional `www`).
- **Admin role** uses SWA's built-in **invitation-based** role assignment (custom-roles-via-function is
  Standard-only). Documented in `DEPLOY.md`.

---

## Key decisions

| Area | Decision |
| --- | --- |
| Hosting | Azure Static Web Apps, **Free** plan, managed Functions API |
| Frontend | Vite + React + TypeScript, React Router, **TanStack Query** (caching/dedupe/prefetch) |
| Backend | Azure Functions **.NET 9 isolated**, C#, HTTP triggers only |
| Storage auth | Fork A: connection string in Azure, `DefaultAzureCredential` locally |
| Data | Table Storage (`Lists`, `ListMovies`, `Discussions`) + Blob (`tmdb-cache`, private) |
| TMDB | v3 REST, Bearer Read Access Token, US region, cached indefinitely in Blob |
| Discussion topics | **AI-authored** via existing **Azure AI Foundry** (`richtercloud-0138-resource`) at admin time; admin edits/approves; stored & served statically |
| IaC | Bicep in `/infra` + `az` script alternative (`provision.ps1`) |
| CI/CD | GitHub Actions: `ci.yml`, `azure-static-web-apps.yml`, `infra.yml` (OIDC) |

---

## Project layout

Scaffolded at the **repository root** (not nested in `movies-app/`, since the workspace folder already
serves as the project root).

```
.
├─ .github/workflows/       # ci.yml, azure-static-web-apps.yml, infra.yml
├─ .vscode/                 # extensions, settings, launch (debug Functions + Vite)
├─ api/                     # .NET 9 isolated Functions (SWA managed API)
│  ├─ MoviesApp.Api.csproj
│  ├─ Program.cs, host.json, local.settings.example.json
│  ├─ Functions/  Services/  Models/  Tests/
├─ src/                     # Vite + React + TS frontend
│  ├─ main.tsx, App.tsx, routes/, components/, lib/, styles/
├─ public/
├─ infra/                   # main.bicep, modules/, main.parameters.json, provision.sh
├─ scripts/                 # dev.sh, seed-list.sh
├─ staticwebapp.config.json
├─ swa-cli.config.json
├─ package.json, vite.config.ts, tsconfig*.json, .eslintrc.cjs, .prettierrc
├─ .editorconfig, .gitattributes, .gitignore
└─ PLAN.md ARCHITECTURE.md DEPLOY.md README.md CONTRIBUTING.md LICENSE
```

---

## Data model (summary — see ARCHITECTURE.md for detail)

- **`Lists`** — PartitionKey=`list`, RowKey=`{listId}`. Fields: `title`, `slug`, `period`, `isActive`,
  `sortOrder`, `createdUtc`.
- **`ListMovies`** — PartitionKey=`{listId}`, RowKey=`{tmdbId}`. Fields: `order`, `notes`, `addedUtc`.
  (The TMDB payload itself lives in Blob cache, keyed by `tmdbId`.)
- **`Discussions`** — PartitionKey=`{tmdbId}`, RowKey=`current`. Fields: `topicsJson`, `source` (`ai|manual`),
  `status` (`draft|published`), `model`, `generatedUtc`, `approvedBy`, `approvedUtc`.
- **Blob `tmdb-cache`** — `movies/{tmdbId}.json` (full normalized TMDB payload), plus optional
  `posters/{tmdbId}.jpg`. Each JSON carries `etag` + `lastFetchedUtc`.

---

## API surface

**Public (anonymous):**
- `GET /api/lists/current` — active list with embedded movie cards (one round-trip; ETag + `Cache-Control`).
- `GET /api/movies/{tmdbId}` — full detail incl. cast, crew, providers, published discussion topics.

**Admin (role `admin`):**
- `GET /api/admin/lists` · `POST /api/admin/lists` · `PATCH /api/admin/lists/{id}` · `DELETE /api/admin/lists/{id}`
- `POST /api/admin/lists/{id}/movies` · `DELETE /api/admin/lists/{id}/movies/{tmdbId}` · `PATCH /api/admin/lists/{id}/movies/{tmdbId}`
- `POST /api/admin/movies/{tmdbId}/refresh` · `POST /api/admin/movies/refresh-bulk` (chunked)
- `GET /api/admin/tmdb/search?q=&year=`
- `POST /api/admin/movies/{tmdbId}/discussion/generate` (Azure OpenAI → **draft**, not auto-published)
- `PATCH /api/admin/movies/{tmdbId}/discussion` (edit / approve / publish)

The authenticated user is read from the SWA `X-MS-CLIENT-PRINCIPAL` header inside Functions.

---

## Phased plan

### Phase 0 — Scaffold & repo hygiene
**Tasks:** git init; full folder tree; `.gitignore`, `.editorconfig`, `.gitattributes`; `README`,
`CONTRIBUTING`, `LICENSE` (MIT); `.vscode/*`; empty/working workflows; `local.settings.example.json`;
`staticwebapp.config.json`; `swa-cli.config.json`; root `package.json`; Vite/TS/ESLint/Prettier configs;
planning docs (this file, `ARCHITECTURE.md`, `DEPLOY.md`).
**Acceptance:** repo builds an empty Vite app; `dotnet build` of the API skeleton succeeds; no secrets tracked.
**Manual test:** `npm install && npm run build`; `dotnet build api`; `git status` shows no ignored secrets.

### Phase 1 — Infrastructure
**Tasks:** author `infra/main.bicep` + modules (`storage`, `swa`, optional `openai`); `main.parameters.json`;
`provision.sh` (az CLI alternative); wire `infra.yml` (OIDC, `what-if` then `create`). Provision RG, Storage
(StorageV2, LRS, TLS 1.2, public blob disabled), tables, container, SWA (Free). **Azure AI Foundry is an
existing resource (`richtercloud-0138-resource`) — referenced, not created**; its endpoint/key/deployment
are set as SWA app settings. Assign `Storage Table/Blob Data Contributor` to **your user** for local
`DefaultAzureCredential` (no SWA MI on managed functions).
**Acceptance:** `az deployment group what-if` clean; resources exist; you can list tables locally via `az login`.
**Manual test:** run `provision.ps1`; confirm resources in portal; `az staticwebapp show`.

### Phase 2 — Functions API
**Tasks:** `.NET 9 isolated` host; `Program.cs` DI; `TmdbClient` (Bearer, US region, watch/providers, polite
rate-limiting + retry); `BlobCacheService`; `ListRepository`, `ListMovieRepository`, `DiscussionRepository`;
`StorageClientFactory`; public `GET /api/lists/current` and `GET /api/movies/{tmdbId}`; xUnit tests.
**Acceptance:** endpoints return cached TMDB data; unit tests pass; cache writes blob with `etag/lastFetchedUtc`.
**Manual test:** `func`/`swa start`, hit `/api/lists/current` and `/api/movies/{id}`.

### Phase 3 — Public SPA + performance
**Tasks:** app shell, list page `/`, detail `/movie/:tmdbId`; route-level code splitting; skeleton cards;
ETag/`Cache-Control` honored by client; LQIP/base64 placeholder → swap to full poster; `<link rel=preload>`
hero; route prefetch; long-cache hashed bundles; `swa start` wires Vite + Functions; mobile-first 390×844;
TMDB attribution footer; filter/sort (genre, rating, runtime, certification).
**Acceptance:** Lighthouse mobile perf budget met; back button preserves scroll; single round-trip list render.
**Manual test:** `swa start`; throttled mobile profile in DevTools.

### Phase 4 — Auth + Admin
**Tasks:** SWA GitHub auth; `staticwebapp.config.json` roles for `/admin/*` and `/api/admin/*`; admin UI
(manage lists, TMDB search + add/remove, drag-reorder, notes); admin API CRUD reading `X-MS-CLIENT-PRINCIPAL`;
chunked refresh endpoints.
**Acceptance:** anonymous gets 401 on admin routes; `admin` role gets 200; CRUD persists to Tables.
**Manual test:** log in via GitHub, accept invite to `admin` role, exercise CRUD.

### Phase 5 — AI discussion topics + seed
**Tasks:** `DiscussionGenerationService` (Azure AI Foundry chat completion via `richtercloud-0138-resource`,
with movie context + teen-family framing); `POST .../discussion/generate` returns a **draft**;
admin edit/approve → publish; `seed-list.sh` posts the Summer 2026 seed list, then admin generates topics
per movie.
**Acceptance:** generated draft is editable; only `published` topics render publicly; never a live LLM call at
request time.
**Manual test:** generate → edit → publish → verify public detail page shows approved text.

### Phase 6 — Deploy + custom domain + hardening
**Tasks:** `azure-static-web-apps.yml` (app `/`, api `api`, output `dist`; verify .NET 9 isolated build —
likely an explicit `dotnet publish` + `api_build_command`); custom domain `movies.mikerichter.app`
(documented in `DEPLOY.md`, not executed); CSP + security headers via `staticwebapp.config.json`; branch
protection (manual).
**Acceptance:** push to `main` deploys; PR previews work; security headers present; domain steps documented.
**Manual test:** open PR → preview URL; merge → production; `curl -I` checks headers.

---

## Open questions

1. **.NET 9 isolated** confirmed acceptable (vs revisiting Standard + BYO Functions for .NET 10 + true MI)?
2. **Connection-string-in-app-settings** (Fork A) residual risk acceptable, or upgrade to Standard now?
3. **Azure AI Foundry** — using existing `richtercloud-0138-resource`. Confirm the **model deployment name**
   to target (e.g. `gpt-4o-mini`) and that admin-time usage cost is acceptable (cached forever).
4. **LICENSE = MIT** acceptable?
5. **`gh repo create`** — repo name `movies-app`, **public or private**? (Command will be output for your
   approval, not run automatically.)
6. **`movies.mikerichter.app`** — apex only, or apex + `www`?
