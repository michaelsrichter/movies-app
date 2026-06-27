#!/usr/bin/env bash
#
# One-shot local dev: install deps, restore/build the API, and start SWA (Vite + Functions).
# Runs against REAL Azure Storage via your `az login` (no Azurite).
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if [[ ! -f api/local.settings.json ]]; then
  echo "WARNING: api/local.settings.json not found." >&2
  echo "  Copy from api/local.settings.example.json and fill in tokens." >&2
fi

echo "Installing npm dependencies..."
npm install

echo "Building API..."
dotnet build api/MoviesApp.Api.csproj

echo "Starting SWA (Vite + Functions)..."
npm run dev:swa
</content>
