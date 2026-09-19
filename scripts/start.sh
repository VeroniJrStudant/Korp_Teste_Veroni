#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

echo "→ Subindo PostgreSQL"
if command -v docker-compose >/dev/null 2>&1; then
  docker-compose up -d postgres
else
  docker compose up -d postgres
fi

echo "→ Aguardando banco"
for i in {1..40}; do
  if docker-compose exec -T postgres pg_isready -U korp >/dev/null 2>&1; then
    break
  fi
  sleep 1
done

trap 'kill $(jobs -p) 2>/dev/null' EXIT

echo "→ API de estoque em http://localhost:5081/swagger"
dotnet run --project "$ROOT/microservices/stock-api/Korp.Stock.Api.csproj" --launch-profile http &
echo "→ API de faturamento em http://localhost:5082/swagger"
dotnet run --project "$ROOT/microservices/billing-api/Korp.Billing.Api.csproj" --launch-profile http &

echo "→ Angular em http://localhost:4200"
cd "$ROOT/frontend"
npm start
