# Korp_Teste_Veroni

Sistema de emissão de Notas Fiscais — desafio técnico **Korp ERP**.

**Angular 19 + dois microsserviços ASP.NET Core (C# / .NET 10) + PostgreSQL.**

Repositório público no formato pedido pelo enunciado: `Korp_Teste_SeuNome`.

## Arquitetura

```
frontend/                 Angular 19 + Angular Material   :4200
microservices/
  stock-api/              Estoque      :5081
  billing-api/            Faturamento  :5082
infra/                    init do PostgreSQL
scripts/                  start.sh e test-concurrency.sh
```

O Angular chama as duas APIs. O faturamento chama o estoque na criação de itens e na impressão.

PostgreSQL 16 em `localhost:5433`, com os bancos `korp_stock` e `korp_billing`.

Cada parte tem o próprio README, com endpoints, decisões e testes:

- [frontend](./frontend/README.md)
- [microsserviço de Estoque](./microservices/stock-api/README.md)
- [microsserviço de Faturamento](./microservices/billing-api/README.md)

## Como executar

Pré-requisitos: Docker (`docker-compose` ou `docker compose`), .NET 10 SDK, Node 22+. No macOS, inicie o runtime Docker (`colima start` ou Docker Desktop).

```bash
chmod +x scripts/start.sh
./scripts/start.sh
```

O script sobe o Postgres, as duas APIs e o Angular.

- App: http://localhost:4200
- Swagger estoque: http://localhost:5081/swagger
- Swagger faturamento: http://localhost:5082/swagger

Em quatro terminais:

```bash
docker-compose up -d postgres
dotnet run --project microservices/stock-api --launch-profile http
dotnet run --project microservices/billing-api --launch-profile http
cd frontend && npm start
```

## O que o sistema faz

1. **Cadastro de produtos** com código, descrição e saldo.
2. **Cadastro de notas** com numeração sequencial, status inicial **Aberta** e vários itens.
3. **Impressão**: botão visível em qualquer nota escolhida, indicador de processamento, fecha a nota Aberta, baixa o saldo (ex.: 10 − 2 = 8). Nota Fechada só reimprime o documento, sem nova baixa.
4. **Falha de microsserviço**: botão *Simular falha do estoque*. A impressão falha com mensagem clara, a nota permanece Aberta e volta a funcionar depois.
5. **Concorrência**: produto `PAR-M8` nasce com saldo 1. Duas notas com qtd 1; só uma imprime.
6. **Idempotência**: reimprimir nota Fechada não baixa estoque de novo; baixas usam `operationId` único.
7. **IA local**: descrição de produto, insights no painel e assistente de domínio.
8. **Interface**: 100% Angular Material (Material Design 3), com tema claro e escuro trocável na barra superior.

## Testes

```bash
dotnet test Korp.Teste.slnx
cd frontend && npm test
```

Os testes de banco usam **PostgreSQL** (Testcontainers ou o compose na porta 5433). Sem Docker, eles são ignorados; os de IA local, gateway HTTP e Angular continuam.

- **Estoque:** cadastro, validação, baixa, idempotência, estorno e `pg_advisory_xact_lock`.
- **Faturamento:** numeração sequencial, impressão, reimpressão idempotente, compensação e gateway HTTP (409/503).
- **Angular:** serviços HTTP, interceptor de erro, `ngOnChanges` da linha de item, numeração da lista e serviço de tema.

## Dados iniciais

| Código | Descrição | Saldo |
|---|---|---|
| ACO-1020 | Barra de aço SAE 1020 | 10 |
| PAR-M8 | Parafuso M8 x 20 | 1 |
| CHP-304 | Chapa inox 304 | 25 |
| DSC-45 | Disco de corte | 50 |
| OLE-20L | Óleo industrial 20L | 8 |

## Detalhamento técnico e roteiro

- [DETALHAMENTO_TECNICO.md](./DETALHAMENTO_TECNICO.md) · [PDF](./DETALHAMENTO_TECNICO.pdf)
- [ROTEIRO_VIDEO.md](./ROTEIRO_VIDEO.md) · [PDF](./ROTEIRO_VIDEO.pdf)

