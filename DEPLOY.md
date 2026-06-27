# Deploy & Operations Guide

Step-by-step provisioning and deployment for the Family Movie Watchlist on **Azure Static Web Apps (Free)**.
No Azurite — local dev runs against **real** Azure Storage via your `az login`.

> Prereqs: `az` CLI (logged in: `az login`), `gh` CLI (optional), Node 20+, .NET 9 SDK, SWA CLI
> (`npm i -g @azure/static-web-apps-cli`), Functions Core Tools v4.

---

## 0. Variables

```bash
export RG="rg-movies"
export LOCATION="eastus2"
export STORAGE="stmovies$RANDOM"          # must be globally unique, lowercase
export SWA_NAME="swa-movies"
export FOUNDRY="richtercloud-0138-resource"   # EXISTING Azure AI Foundry resource (not created here)
export SUBSCRIPTION="$(az account show --query id -o tsv)"
```

---

## 1. Provision infrastructure

### Option A — Bicep (preferred)

```bash
az group create -n "$RG" -l "$LOCATION"

az deployment group what-if -g "$RG" \
  -f infra/main.bicep -p infra/main.parameters.json \
  -p storageAccountName="$STORAGE" staticWebAppName="$SWA_NAME"

az deployment group create -g "$RG" \
  -f infra/main.bicep -p infra/main.parameters.json \
  -p storageAccountName="$STORAGE" staticWebAppName="$SWA_NAME"
```

### Option B — az CLI script

```bash
./infra/provision.sh -g "$RG" -l "$LOCATION" -s "$STORAGE" -n "$SWA_NAME"
```

This creates: Storage (StorageV2, LRS, TLS 1.2, public blob access disabled), tables `Lists`,
`ListMovies`, `Discussions`, blob container `tmdb-cache` (private), and the Static Web App (Free).

> **Azure AI Foundry** (`richtercloud-0138-resource`) already exists and is **not** created by this template.

---

## 2. Storage role assignment for local development

`DefaultAzureCredential` uses your `az login` identity locally. Grant your user data-plane access (no keys):

```bash
ME="$(az ad signed-in-user show --query id -o tsv)"
SCOPE="$(az storage account show -g "$RG" -n "$STORAGE" --query id -o tsv)"

az role assignment create --assignee "$ME" \
  --role "Storage Table Data Contributor" --scope "$SCOPE"
az role assignment create --assignee "$ME" \
  --role "Storage Blob Data Contributor" --scope "$SCOPE"
```

> Managed Functions on SWA have **no managed identity**, so there is no SWA identity to grant. In Azure,
> the API authenticates to Storage with a connection string (next step).

---

## 3. Application settings (secrets — never committed)

Get the Foundry endpoint/key and a Storage connection string, then set them on the SWA resource:

```bash
# Storage connection string (used by the API in Azure)
STG_CONN="$(az storage account show-connection-string -g "$RG" -n "$STORAGE" -o tsv)"

# Azure AI Foundry endpoint + key (existing resource)
AI_ENDPOINT="$(az cognitiveservices account show -n "$FOUNDRY" -g <foundry-rg> --query properties.endpoint -o tsv)"
AI_KEY="$(az cognitiveservices account keys list -n "$FOUNDRY" -g <foundry-rg> --query key1 -o tsv)"

az staticwebapp appsettings set -n "$SWA_NAME" --setting-names \
  Storage__ConnectionString="$STG_CONN" \
  Storage__AccountName="$STORAGE" \
  Tmdb__ReadAccessToken="<TMDB_BEARER_TOKEN>" \
  AzureAI__Endpoint="$AI_ENDPOINT" \
  AzureAI__Deployment="gpt-4o-mini" \
  AzureAI__ApiKey="$AI_KEY"
```

> Replace `<foundry-rg>` with the resource group that holds `richtercloud-0138-resource`.

---

## 4. Local development

1. Copy the template and fill in secrets (gitignored):
   ```bash
   cp api/local.settings.example.json api/local.settings.json
   # edit api/local.settings.json: Tmdb__ReadAccessToken, AzureAI__*, Storage__AccountName
   # leave Storage__ConnectionString EMPTY locally so DefaultAzureCredential (az login) is used
   ```
2. Run end-to-end:
   ```bash
   npm install
   dotnet build api
   npm run dev:swa   # wraps: swa start (Vite + Functions host)
   ```
3. Open the SWA CLI URL (default http://localhost:4280). The SPA, `/api/*`, and emulated auth all work.

---

## 5. CI/CD secrets (GitHub repo → Settings → Secrets and variables → Actions)

| Secret | Used by | Notes |
| --- | --- | --- |
| `AZURE_STATIC_WEB_APPS_API_TOKEN` | `azure-static-web-apps.yml` | `az staticwebapp secrets list -n $SWA_NAME --query properties.apiKey -o tsv` |
| `AZURE_CLIENT_ID` | `infra.yml` (OIDC) | app registration client id |
| `AZURE_TENANT_ID` | `infra.yml` | |
| `AZURE_SUBSCRIPTION_ID` | `infra.yml` | |
| `TMDB_READ_ACCESS_TOKEN` | optional | only if injecting app settings via Action |

### OIDC federated credential (no client secret stored)

```bash
APP_ID="$(az ad app create --display-name 'movies-oidc' --query appId -o tsv)"
az ad sp create --id "$APP_ID"
az ad app federated-credential create --id "$APP_ID" --parameters '{
  "name": "github-main",
  "issuer": "https://token.actions.githubusercontent.com",
  "subject": "repo:<owner>/<repo>:ref:refs/heads/main",
  "audiences": ["api://AzureADTokenExchange"]
}'
az role assignment create --assignee "$APP_ID" --role "Contributor" \
  --scope "/subscriptions/$SUBSCRIPTION/resourceGroups/$RG"
```

---

## 6. Assign yourself the `admin` role (SWA invitations)

SWA Free uses **invitation-based** role assignment:

1. Azure portal → your Static Web App → **Role management** → **Invite**.
2. Authorization provider: **GitHub**; enter your GitHub username; role: `admin`; generate the invite link.
3. Open the link while signed in to GitHub and accept. You now hold the `admin` role.
4. `staticwebapp.config.json` enforces `admin` on `/admin/*` and `/api/admin/*`.

---

## 7. First deploy

Pushing to `main` triggers `azure-static-web-apps.yml` (build + deploy; PR previews are free).
Verify the `.NET 9 isolated` build — if the SWA action's built-in build is insufficient, the workflow runs an
explicit `dotnet publish -c Release` and sets `api_build_command`.

Seed the first list after the API is live:

```bash
./scripts/seed-list.sh "https://<your-swa>.azurestaticapps.net"
# then in /admin, click "Generate" per movie to author discussion topics, edit, and Publish.
```

---

## 8. Custom domain — `movies.mikerichter.app` (documented, not executed)

Free tier allows **2 custom domains per app**.

1. SWA → **Custom domains** → **Add** → enter `movies.mikerichter.app`.
2. SWA shows a validation record. At your DNS provider for `mikerichter.app`, add:
   - **CNAME** `movies` → `<your-swa>.azurestaticapps.net` (subdomain), **or**
   - For an apex, use the **ALIAS/ANAME** or Azure-provided TXT validation + CNAME flattening.
3. Back in the portal, **Validate**; SWA issues a free auto-renewing SSL cert.
4. (Optional) add `www.movies.mikerichter.app` as the 2nd domain with a redirect.

> Do not run the DNS change as part of deploy — perform it manually when ready to cut over.

---

## 9. Branch protection (manual, after first push)

GitHub → repo **Settings → Branches → Add rule** for `main`:
- Require a pull request before merging.
- Require status checks to pass (`ci`).
- Include administrators (optional).

---

## 10. Troubleshooting

- **401 on `/api/admin/*`** → not in `admin` role; re-check the invite (step 6).
- **403/`AuthorizationFailure` from Storage locally** → missing data-plane role (step 2) or stale `az login`.
- **Bulk refresh times out** → keep chunks small; the 45 s API limit is hard on SWA.
- **.NET 10 errors** → managed Functions only support up to .NET 9 isolated (by design).
