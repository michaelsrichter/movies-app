#!/usr/bin/env bash
#
# Provisions Azure resources for the Family Movie Watchlist (az CLI alternative to Bicep).
# Requires: az CLI (logged in via `az login`).
#
# Notes:
#   - Azure AI Foundry (richtercloud-0138-resource) is an EXISTING resource and is NOT created here.
#   - Managed Functions on SWA Free have no managed identity; the API uses a Storage connection string
#     in Azure, while local dev uses your `az login` (DefaultAzureCredential).
#
# Usage:
#   ./infra/provision.sh -g <resource-group> -s <storage-account> -n <swa-name> [-l <location>]
set -euo pipefail

LOCATION="eastus2"
RESOURCE_GROUP=""
STORAGE_ACCOUNT=""
STATIC_WEB_APP_NAME=""

usage() {
  echo "Usage: $0 -g <resource-group> -s <storage-account> -n <swa-name> [-l <location>]" >&2
  exit 1
}

while getopts "g:s:n:l:h" opt; do
  case "$opt" in
    g) RESOURCE_GROUP="$OPTARG" ;;
    s) STORAGE_ACCOUNT="$OPTARG" ;;
    n) STATIC_WEB_APP_NAME="$OPTARG" ;;
    l) LOCATION="$OPTARG" ;;
    *) usage ;;
  esac
done

[[ -z "$RESOURCE_GROUP" || -z "$STORAGE_ACCOUNT" || -z "$STATIC_WEB_APP_NAME" ]] && usage

echo "Creating resource group $RESOURCE_GROUP..."
az group create -n "$RESOURCE_GROUP" -l "$LOCATION" >/dev/null

echo "Creating storage account $STORAGE_ACCOUNT..."
az storage account create \
  -n "$STORAGE_ACCOUNT" -g "$RESOURCE_GROUP" -l "$LOCATION" \
  --sku Standard_LRS --kind StorageV2 \
  --min-tls-version TLS1_2 --allow-blob-public-access false \
  --https-only true >/dev/null

echo "Creating tables and blob container..."
for t in Lists ListMovies Discussions; do
  az storage table create --name "$t" --account-name "$STORAGE_ACCOUNT" --auth-mode login >/dev/null
done
az storage container create --name tmdb-cache --account-name "$STORAGE_ACCOUNT" \
  --auth-mode login --public-access off >/dev/null

echo "Creating Static Web App (Free)..."
az staticwebapp create -n "$STATIC_WEB_APP_NAME" -g "$RESOURCE_GROUP" -l "$LOCATION" --sku Free >/dev/null

echo "Granting your user data-plane access for local DefaultAzureCredential..."
ME="$(az ad signed-in-user show --query id -o tsv)"
SCOPE="$(az storage account show -g "$RESOURCE_GROUP" -n "$STORAGE_ACCOUNT" --query id -o tsv)"
az role assignment create --assignee "$ME" --role "Storage Table Data Contributor" --scope "$SCOPE" >/dev/null
az role assignment create --assignee "$ME" --role "Storage Blob Data Contributor" --scope "$SCOPE" >/dev/null

echo "Done. Next: set app settings (see DEPLOY.md section 3)."
