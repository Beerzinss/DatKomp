#!/usr/bin/env bash
set -euo pipefail

if docker compose version >/dev/null 2>&1; then
  COMPOSE=(docker compose)
else
  COMPOSE=(docker-compose)
fi

"${COMPOSE[@]}" --env-file .env up -d --build --remove-orphans
"${COMPOSE[@]}" --env-file .env ps
