# Korp.Stock.Api — microsserviço de Estoque

Dono do catálogo de produtos e do **saldo**. É o único serviço que altera quantidade em estoque.

**ASP.NET Core 10 · Entity Framework Core + Npgsql · PostgreSQL `korp_stock` · porta 5081**

- Swagger: http://localhost:5081/swagger
- Requisições prontas: [`Korp.Stock.Api.http`](./Korp.Stock.Api.http)

## Responsabilidades

| Faz | Não faz |
|---|---|
| Cadastro e edição de produtos | Conhecer notas fiscais |
| Baixa (`debit`) e estorno (`credit`) com idempotência | Decidir *quando* baixar — quem decide é o faturamento |
| Lock de concorrência por produto | Chamar outros serviços |
| Modo de falha simulada para a demonstração | — |

O serviço não tem dependência de saída: ele é chamado, nunca chama.

## Endpoints

### Produtos

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/products?search=` | Lista ordenada por código; busca por código ou descrição |
| `GET` | `/api/products/{id}` | Detalhe do produto |
| `POST` | `/api/products` | Cadastra (código único, normalizado em maiúsculas) |
| `PUT` | `/api/products/{id}` | Atualiza descrição e saldo |
| `POST` | `/api/products/{id}/debit` | Baixa de saldo |
| `POST` | `/api/products/{id}/credit` | Estorno de uma baixa |

### Operações e IA local

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/health` | `healthy` ou `degraded` quando a falha simulada está ativa |
| `GET` | `/api/chaos` | Estado atual da falha simulada |
| `POST` | `/api/chaos` | Liga/desliga a falha simulada |
| `POST` | `/api/ai/suggest-description` | Gera descrição comercial a partir do código/nome |

### Exemplos

```http
POST /api/products
Content-Type: application/json

{ "code": "ACO-1045", "description": "Barra de aço SAE 1045", "balance": 12 }
```

```http
POST /api/products/{id}/debit
Content-Type: application/json

{ "quantity": 2, "operationId": "invoice:6f2a...:product:1b8c..." }
```

Resposta:

```json
{
  "productId": "1b8c...",
  "code": "ACO-1020",
  "balance": 8,
  "operationId": "invoice:6f2a...:product:1b8c...",
  "idempotentReplay": false
}
```

## Concorrência

A baixa roda dentro de transação com lock consultivo por produto:

```csharp
var lockKey = BitConverter.ToInt64(productId.ToByteArray(), 0);
await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockKey})", ct);
```

Duas impressões simultâneas do mesmo produto entram em fila: a primeira baixa, a segunda encontra saldo insuficiente e recebe **409**. O lock é liberado no fim da transação, sem risco de travar sozinho.

O `IsNpgsql()` existe porque o lock é específico do PostgreSQL; em outro provedor o código segue sem ele.

## Idempotência

Cada baixa carrega um `operationId` (o faturamento envia `invoice:{id}:product:{id}`), gravado em `stock_movements`:

- **Baixa repetida** com o mesmo `operationId` não desconta de novo e volta com `idempotentReplay: true`.
- **Estorno** procura o movimento original, devolve exatamente aquela quantidade e apaga o registro. Estorno sem movimento correspondente é no-op.

## Banco `korp_stock`

| Tabela | Conteúdo | Índice |
|---|---|---|
| `products` | código, descrição, saldo, datas | `code` **único** |
| `stock_movements` | histórico de baixas, com produto, tipo e quantidade | `operation_id` **único** |

O índice único em `operation_id` é a garantia de idempotência no nível do banco: mesmo em corrida, a segunda tentativa de gravar a mesma operação não passa.

## Falha simulada

`POST /api/chaos { "enabled": true }` faz o `ChaosMiddleware` responder **503** em todas as rotas de negócio. `/api/health` e `/api/chaos` continuam no ar, para o painel exibir o estado e permitir a restauração.

É assim que o requisito “um microsserviço fora do ar” é demonstrado sem derrubar o processo.

## Erros

`AppException` de domínio → middleware global → **RFC 7807 ProblemDetails**.

| Código | HTTP | Quando |
|---|---|---|
| `VALIDATION` | 400 | código/descrição vazios, saldo negativo, quantidade ≤ 0, `operationId` ausente |
| `NOT_FOUND` | 404 | produto inexistente |
| `CONFLICT` | 409 | código de produto duplicado |
| `INSUFFICIENT_STOCK` | 409 | saldo menor que o solicitado |
| `STOCK_UNAVAILABLE` | 503 | falha simulada ativa |
| `INTERNAL` | 500 | fallback, logado e sem stack trace na resposta |

## Estrutura

```
stock-api/
├── Controllers/     ProductsController, AiController, OpsController
├── Services/        ProductService (regra de negócio), CatalogAiService
├── Domain/          Product, StockMovement
├── Data/            StockDbContext, StockSeeder
├── Contracts/       DTOs de request e response
├── Middleware/      ExceptionHandling, Chaos
├── Errors/          AppException + fábricas (StockErrors)
└── Infrastructure/  ChaosState (singleton)
```

## Configuração

`appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Stock": "Host=localhost;Port=5433;Database=korp_stock;Username=korp;Password=korp"
  }
}
```

CORS liberado apenas para `http://localhost:4200`. Na subida o serviço roda `EnsureCreated` e semeia o catálogo:

| Código | Descrição | Saldo |
|---|---|---|
| ACO-1020 | Barra de aço SAE 1020 | 10 |
| PAR-M8 | Parafuso sextavado M8 | **1** |
| CHP-304 | Chapa inox 304 | 25 |
| DSC-45 | Disco de corte | 50 |
| OLE-20L | Óleo lubrificante 20L | 8 |

O `PAR-M8` nasce com saldo 1 de propósito: é o produto usado na demonstração de concorrência.

## Rodar

```bash
docker-compose up -d postgres
dotnet run --project microservices/stock-api --launch-profile http
```

## Testes

```bash
dotnet test microservices/stock-api.tests
```

16 testes cobrindo cadastro e validação, baixa, replay idempotente, estorno, saldo insuficiente, a IA local de descrição e o `pg_advisory_xact_lock` com duas baixas simultâneas no `PAR-M8`.

Os testes de integração usam **PostgreSQL de verdade** (Testcontainers ou o container do `docker-compose` na porta 5433) — nada de SQLite in-memory, porque o lock consultivo não existiria lá.
