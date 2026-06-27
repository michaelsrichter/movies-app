#!/usr/bin/env bash
#
# Generates AI discussion topics for every movie in the active list and publishes them.
# Generation uses Azure AI Foundry (gpt-5.4) at admin time; the published topics are stored and
# served statically. Re-run to regenerate.
#
# Usage:
#   ./scripts/seed-topics.sh [base-url] [approved-by]
#     base-url defaults to http://localhost:7071 (func start)
#
# Requires: curl, jq.
set -euo pipefail

BASE_URL="${1:-http://localhost:7071}"
APPROVED_BY="${2:-michaelsrichter}"

command -v jq >/dev/null || { echo "jq is required" >&2; exit 1; }

mapfile -t IDS < <(curl -fsS "$BASE_URL/api/lists/current" | jq -r '.movies[].tmdbId')

echo "Generating + publishing topics for ${#IDS[@]} movies..."
for id in "${IDS[@]}"; do
  echo -n "  tmdb $id: generating... "
  n="$(curl -fsS -X POST "$BASE_URL/api/manage/movies/$id/discussion/generate" | jq -r '.topics | length')"
  echo -n "$n topics; publishing... "
  curl -fsS -X PATCH "$BASE_URL/api/manage/movies/$id/discussion" \
    -H 'Content-Type: application/json' \
    -d "{\"status\":\"published\",\"approvedBy\":\"$APPROVED_BY\"}" >/dev/null
  echo "done"
done

echo "All topics generated and published."
