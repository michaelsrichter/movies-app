#!/usr/bin/env bash
#
# Seeds the "Summer 2026" watchlist by POSTing to the admin API.
#
# Requires an authenticated session with the `admin` role. For local dev against the SWA CLI, the
# emulator injects a principal; in production you must be signed in and hold the `admin` role.
# After seeding, open /admin and click "Generate" per movie to author discussion topics.
#
# Usage:
#   ./scripts/seed-list.sh [base-url]
#     base-url defaults to http://localhost:4280 (SWA CLI)
#
# Requires: curl, jq.
set -euo pipefail

BASE_URL="${1:-http://localhost:4280}"

command -v jq >/dev/null || { echo "jq is required" >&2; exit 1; }

# "tmdbId|title" for the Summer 2026 seed list.
# NOTE: verify each TMDB id against TMDB before production use; these are best-effort.
SEED=(
  "9647|The Last of the Mohicans (1992)"
  "11975|The Age of Innocence (1993)"
  "3131|Gangs of New York (2002)"
  "7345|There Will Be Blood (2007)"
  "770|Gone with the Wind (1939)"
  "9587|Little Women (1994)"
  "64682|The Great Gatsby (2013)"
  "8489|Cinderella Man (2005)"
  "15252|School Ties (1992)"
  "207|Dead Poets Society (1989)"
  "489|Good Will Hunting (1997)"
  "11020|October Sky (1999)"
  "1366|Rocky (1976)"
  "9056|Seabiscuit (2003)"
  "273|O Brother, Where Art Thou? (2000)"
)

echo "Creating list 'Summer 2026'..."
LIST_ID="$(curl -fsS -X POST "$BASE_URL/api/admin/lists" \
  -H 'Content-Type: application/json' \
  -d '{"title":"Summer 2026","slug":"summer-2026","period":"Summer 2026","isActive":true}' \
  | jq -r '.id')"
echo "List id: $LIST_ID"

order=0
for entry in "${SEED[@]}"; do
  tmdb_id="${entry%%|*}"
  title="${entry#*|}"
  echo "Adding $title..."
  curl -fsS -X POST "$BASE_URL/api/admin/lists/$LIST_ID/movies" \
    -H 'Content-Type: application/json' \
    -d "{\"tmdbId\":$tmdb_id,\"order\":$order}" >/dev/null
  order=$((order + 1))
done

echo "Seed complete. Open /admin to refresh TMDB data and generate discussion topics."
</content>
