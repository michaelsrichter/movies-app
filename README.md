# Family Movie Watchlist

A mobile-first family movie watchlist with rich TMDB-backed detail pages and **AI-authored family discussion
topics** for parents of teenagers. Hosted on **Azure Static Web Apps (Free)** with a managed **.NET 9
isolated** Functions API, Azure Table + Blob Storage, and Azure AI Foundry. Public site eventually at
**https://movies.mikerichter.app**.

## Stack

| Layer | Tech |
| --- | --- |
| Frontend | Vite + React + TypeScript, React Router, TanStack Query |
| Backend | Azure Functions, .NET 9 isolated (SWA managed API) |
| Data | Azure Table Storage + Blob Storage (TMDB cache) |
| Auth | SWA built-in auth (GitHub), `admin` role |
| AI | Azure AI Foundry (`richtercloud-0138-resource`) — admin-time topic generation |

## Quick start (local)

```bash
cp api/local.settings.example.json api/local.settings.json   # fill in tokens (gitignored)
npm install
dotnet build api
npm run dev:swa        # swa start → Vite + Functions, real Azure Storage via your `az login`
```

Open http://localhost:4280. No Azurite — local Functions use `DefaultAzureCredential` against real Storage.

## Documentation

- [PLAN.md](PLAN.md) — phased implementation plan and resolved platform constraints.
- [ARCHITECTURE.md](ARCHITECTURE.md) — component/sequence diagrams, data model, caching.
- [DEPLOY.md](DEPLOY.md) — provisioning, secrets, custom domain, admin role.
- [CONTRIBUTING.md](CONTRIBUTING.md) — commit style and local checks.

## Attribution

This product uses the TMDB API but is not endorsed or certified by TMDB.

## License

[MIT](LICENSE)
