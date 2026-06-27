# Architecture

Family movie watchlist on Azure Static Web Apps (Free) with a managed **.NET 9 isolated** Functions API,
Azure Table + Blob Storage, TMDB integration, and Azure OpenAI for admin-time discussion-topic generation.

> See `PLAN.md` → **Resolved constraints** for why Managed Identity and .NET 10 were not used.

## Component diagram

```mermaid
flowchart TB
    subgraph Browser["Browser (mobile-first SPA)"]
        UI["React + Vite + TS<br/>React Router · TanStack Query"]
    end

    subgraph SWA["Azure Static Web Apps (Free)"]
        CDN["Static assets / CDN<br/>(hashed bundles, long cache)"]
        AUTH["Built-in Auth<br/>(GitHub provider)"]
        API["Managed Functions API<br/>.NET 9 isolated · /api/*"]
    end

    subgraph Azure["Azure resources"]
        TBL["Table Storage<br/>Lists · ListMovies · Discussions"]
        BLOB["Blob Storage<br/>tmdb-cache (private)"]
        AOAI["Azure AI Foundry<br/>richtercloud-0138-resource"]
    end

    TMDB["TMDB v3 REST API<br/>(Bearer token)"]

    UI -->|"GET /api/*"| API
    UI --> CDN
    UI -.->|"/.auth/login/github"| AUTH
    API -->|"conn string (Azure) /<br/>DefaultAzureCredential (local)"| TBL
    API --> BLOB
    API -->|"cache miss / refresh"| TMDB
    API -->|"admin-time generate"| AOAI
    AUTH -->|"X-MS-CLIENT-PRINCIPAL"| API
```

## Request sequence — public list page

```mermaid
sequenceDiagram
    participant B as Browser
    participant S as SWA (CDN/API)
    participant F as Functions (.NET 9)
    participant T as Table Storage
    participant Bl as Blob (tmdb-cache)

    B->>S: GET / (app shell)
    S-->>B: index.html + hashed JS (skeleton renders)
    B->>F: GET /api/lists/current (If-None-Match: etag)
    alt ETag matches
        F-->>B: 304 Not Modified
    else fresh
        F->>T: query active list + ListMovies
        F->>Bl: read movies/{id}.json (cached TMDB)
        F-->>B: 200 list payload + ETag + Cache-Control
    end
    B->>B: hydrate cards, swap LQIP → full poster
```

## Auth flow (admin)

```mermaid
sequenceDiagram
    participant B as Browser
    participant S as SWA Auth
    participant F as Functions

    B->>S: GET /.auth/login/github
    S-->>B: GitHub OAuth → session cookie
    B->>F: GET /api/admin/lists (cookie)
    Note over S,F: SWA enforces `admin` role on /api/admin/* via staticwebapp.config.json
    S->>F: forwards request + X-MS-CLIENT-PRINCIPAL (base64 JSON)
    F->>F: decode principal, assert `admin` role
    F-->>B: 200 (or 401/403 if not admin)
```

## Caching strategy

```mermaid
flowchart LR
    R["Admin refresh<br/>or first request"] --> M{"Blob cache<br/>exists?"}
    M -- "yes" --> Serve["Serve cached JSON"]
    M -- "no" --> Fetch["Fetch TMDB<br/>(US region, append_to_response)"]
    Fetch --> Norm["Normalize → JSON<br/>+ etag + lastFetchedUtc"]
    Norm --> Store["Write Blob tmdb-cache"]
    Store --> Serve
    Serve --> HTTP["HTTP ETag + Cache-Control<br/>(client + CDN)"]
```

- TMDB cache is **indefinite** (data changes rarely). Refresh is **manual** (per-movie or bulk).
- Bulk refresh is **chunked** to respect the **45 s** SWA API timeout and TMDB rate limits.
- The browser never calls TMDB or Storage directly — all access is server-side.

## Data model

### Table `Lists`
| Key/Field | Type | Notes |
| --- | --- | --- |
| PartitionKey | string | constant `"list"` |
| RowKey | string | `listId` (guid/slug) |
| title | string | e.g. "Summer 2026" |
| slug | string | URL slug |
| period | string | display period |
| isActive | bool | only one active at a time |
| sortOrder | int | |
| createdUtc | datetime | |

### Table `ListMovies`
| Key/Field | Type | Notes |
| --- | --- | --- |
| PartitionKey | string | `listId` |
| RowKey | string | `tmdbId` |
| order | int | card ordering |
| notes | string | curator notes |
| addedUtc | datetime | |

### Table `Discussions`
| Key/Field | Type | Notes |
| --- | --- | --- |
| PartitionKey | string | `tmdbId` |
| RowKey | string | constant `"current"` |
| topicsJson | string | serialized topic list |
| source | string | `ai` \| `manual` |
| status | string | `draft` \| `published` (public renders `published` only) |
| model | string | e.g. `gpt-4o-mini` |
| generatedUtc | datetime | |
| approvedBy | string | admin GitHub login |
| approvedUtc | datetime | |

### Blob `tmdb-cache` (private)
- `movies/{tmdbId}.json` — normalized TMDB payload. We capture **greedily**: title, original title,
  language, year, runtime, US certification + all US release dates, overview, tagline, status, homepage,
  imdb id, budget/revenue, popularity, vote avg/count, genres, keywords, spoken languages, production
  companies/countries, collection, full cast + crew (with ids, order, profiles), and US watch/providers —
  plus the **complete raw TMDB response** retained verbatim. Each payload carries `etag` + `lastFetchedUtc`.
  Fields are stored even when the app does not surface them yet, so new features need no re-fetch.
- `posters/{tmdbId}.jpg` — optional downloaded thumbnail for fast perceived load.

## Local development

```mermaid
flowchart LR
    Dev["swa start"] --> Vite["Vite dev server :5173"]
    Dev --> Func["Functions host :7071 (.NET 9 isolated)"]
    Func -->|"DefaultAzureCredential<br/>(az login → AzureCliCredential)"| AzStg["REAL Azure Storage"]
    Func -->|"key from local.settings.json"| AzAI["Azure AI Foundry<br/>richtercloud-0138-resource"]
```

No Azurite. Local Functions reach **real** Azure Storage via `DefaultAzureCredential`, which resolves your
`az login` (Azure CLI credential). TMDB and Azure AI Foundry (`richtercloud-0138-resource`) use tokens from
the gitignored `api/local.settings.json`.
