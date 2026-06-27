#!/usr/bin/env bash
#
# Seeds the "Summer 2026" watchlist by POSTing to the admin API.
#
# Resolves each movie's TMDB id via the search endpoint (title + year) so ids are always correct,
# then creates the list and adds each movie (which caches the TMDB payload server-side).
#
# Requires an authenticated session with the `admin` role in production. Against a local Functions
# host (func start) or SWA CLI the routes are reachable directly.
#
# Usage:
#   ./scripts/seed-list.sh [base-url]
#     base-url defaults to http://localhost:7071 (func start). Use http://localhost:4280 for `swa start`.
#
# Requires: curl, jq.
set -euo pipefail

BASE_URL="${1:-http://localhost:7071}"

command -v jq >/dev/null || { echo "jq is required" >&2; exit 1; }

# "title|year" for the Summer 2026 seed list.
SEED=(
  "The Last of the Mohicans|1992"
  "The Age of Innocence|1993"
  "Gangs of New York|2002"
  "There Will Be Blood|2007"
  "Gone with the Wind|1939"
  "Little Women|1994"
  "The Great Gatsby|2013"
  "Cinderella Man|2005"
  "School Ties|1992"
  "Dead Poets Society|1989"
  "Good Will Hunting|1997"
  "October Sky|1999"
  "Rocky|1976"
  "Seabiscuit|2003"
  "O Brother, Where Art Thou?|2000"
)

echo "Creating list 'Summer 2026'..."
LIST_ID="$(curl -fsS -X POST "$BASE_URL/api/manage/lists" \
  -H 'Content-Type: application/json' \
  -d '{"title":"Summer 2026","slug":"summer-2026","period":"Summer 2026","isActive":true}' \
  | jq -r '.id')"
echo "List id: $LIST_ID"

order=0
for entry in "${SEED[@]}"; do
  title="${entry%%|*}"
  year="${entry#*|}"

  # URL-encode the title query via jq.
  q="$(jq -rn --arg t "$title" '$t|@uri')"
  tmdb_id="$(curl -fsS "$BASE_URL/api/manage/tmdb/search?q=$q&year=$year" \
    | jq -r '.results[0].id // empty')"

  if [[ -z "$tmdb_id" ]]; then
    echo "  ! Could not resolve TMDB id for '$title ($year)'. Skipping." >&2
    continue
  fi

  echo "Adding $title ($year) -> tmdb $tmdb_id ..."
  curl -fsS -X POST "$BASE_URL/api/manage/lists/$LIST_ID/movies" \
    -H 'Content-Type: application/json' \
    -d "{\"tmdbId\":$tmdb_id,\"order\":$order}" >/dev/null
  order=$((order + 1))
done

echo "Seed complete. Active list 'Summer 2026' with $order movies cached."
