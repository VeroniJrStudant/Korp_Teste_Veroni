#!/usr/bin/env bash
set -euo pipefail
STOCK=http://localhost:5081
BILLING=http://localhost:5082

echo "Produto PAR-M8:"
PAR=$(curl -s "$STOCK/api/products" | python3 -c "import sys,json; ps=json.load(sys.stdin); p=next(x for x in ps if x['code']=='PAR-M8'); print(p['id'], p['balance'])")
echo "$PAR"
PID=$(echo "$PAR" | awk '{print $1}')

echo "Criando duas notas com quantidade 1"
A=$(curl -s -X POST "$BILLING/api/invoices" -H 'Content-Type: application/json' -d "{\"items\":[{\"productId\":\"$PID\",\"quantity\":1}]}")
B=$(curl -s -X POST "$BILLING/api/invoices" -H 'Content-Type: application/json' -d "{\"items\":[{\"productId\":\"$PID\",\"quantity\":1}]}")
AID=$(python3 -c "import json,os,sys; print(json.loads('''$A''')['id'])")
BID=$(python3 -c "import json; print(json.loads('''$B''')['id'])")
echo "Notas $AID e $BID"

echo "Imprimindo em paralelo"
curl -s -o /tmp/print-a.json -w "A HTTP %{http_code}\n" -X POST "$BILLING/api/invoices/$AID/print" &
curl -s -o /tmp/print-b.json -w "B HTTP %{http_code}\n" -X POST "$BILLING/api/invoices/$BID/print" &
wait
echo "--- A ---"; cat /tmp/print-a.json; echo
echo "--- B ---"; cat /tmp/print-b.json; echo
echo "Saldo atual:"; curl -s "$STOCK/api/products/$PID"; echo
