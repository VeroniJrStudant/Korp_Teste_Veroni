# Korp.Billing.Api — microsserviço de Faturamento

Dono da nota fiscal: numeração, itens, status e **impressão**. É quem decide quando o estoque deve ser baixado.

**ASP.NET Core 10 · Entity Framework Core + Npgsql · PostgreSQL `korp_billing` · porta 5082**

- Swagger: http://localhost:5082/swagger
- Requisições prontas: [`Korp.Billing.Api.http`](./Korp.Billing.Api.http)

## Responsabilidades

| Faz | Não faz |
|---|---|
| Numeração sequencial da nota | Guardar saldo de produto |
| Ciclo Aberta → Fechada | Alterar estoque diretamente |
| Impressão com baixa no estoque e compensação | Cadastrar produtos |
| Insights e assistente sobre o próprio domínio | — |

É o **único** serviço com dependência de saída: chama o estoque por HTTP.

## Regra central

A nota nasce **Aberta** e o estoque **não** é tocado no cadastro. A baixa acontece só na **impressão**:

```
POST /api/invoices/{id}/print
   │
   ├─ nota já Fechada? → devolve sucesso, sem nova baixa  (idempotência)
   ├─ nota sem itens?  → 400
   │
   └─ para cada item: POST {estoque}/api/products/{id}/debit
         │
         ├─ tudo ok      → Status = Fechada, ClosedAt = agora
         └─ algum falhou → estorna as baixas já feitas (ordem inversa)
                            e a nota PERMANECE Aberta
```

A compensação usa `Enumerable.Reverse` sobre as baixas aplicadas. Se o próprio estorno falhar, o erro é logado e a exceção original continua subindo — o usuário nunca recebe um “deu certo” falso.

## Endpoints

### Notas fiscais

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/invoices` | Lista, mais recentes primeiro |
| `GET` | `/api/invoices/{id}` | Detalhe com itens |
| `POST` | `/api/invoices` | Cria nota **Aberta** com numeração sequencial |
| `PUT` | `/api/invoices/{id}` | Substitui itens (só se Aberta) |
| `POST` | `/api/invoices/{id}/print` | Fecha a nota e baixa o estoque |

### Operações e IA local

| Método | Rota | Descrição |
|---|---|---|
| `GET` | `/api/health` | Saúde do serviço |
| `GET` | `/api/ai/insights` | Fila de faturamento, saldo crítico e risco de impressão |
| `POST` | `/api/ai/ask` | Assistente por intenção, usando dados reais |

### Exemplos

```http
POST /api/invoices
Content-Type: application/json

{ "items": [ { "productId": "1b8c...", "quantity": 2 } ] }
```

```http
POST /api/invoices/{id}/print
Idempotency-Key: print-6f2a
```

Resposta:

```json
{
  "invoice": { "number": 6, "status": "Fechada", "closedAt": "2026-09-19T20:31:00Z", "items": [] },
  "idempotentReplay": false,
  "message": "Impressão concluída. Status atualizado para Fechada e saldos baixados no estoque."
}
```

## Numeração sequencial

Não usa `MAX(number) + 1`. Há uma tabela `invoice_sequences` com uma linha; a criação abre transação, incrementa `LastNumber` e grava a nota no mesmo commit. Duas criações concorrentes não repetem número.

## Idempotência

Três camadas, porque impressão duplicada é o erro mais caro aqui:

1. **Status**: nota já Fechada devolve `idempotentReplay: true` sem chamar o estoque.
2. **`operationId` por item**: `invoice:{invoiceId}:product:{productId}`. Mesmo que a chamada chegue duas vezes ao estoque, o saldo cai uma vez só.
3. **Header `Idempotency-Key`**: guardado em `LastIdempotencyKey`, para rastreabilidade.

No frontend, o `exhaustMap` ainda impede o duplo clique antes de sair do browser.

## Banco `korp_billing`

| Tabela | Conteúdo | Índice |
|---|---|---|
| `invoices` | número, status, datas, última chave de idempotência | `number` **único** |
| `invoice_items` | produto, código, descrição e quantidade por nota | — |
| `invoice_sequences` | linha única com o último número emitido | — |

O código do produto é copiado para o item no momento da criação. A nota impressa continua legível mesmo que o produto mude de descrição depois.

## Resiliência (Polly)

O `HttpClient` do `StockGateway` usa `AddStandardResilienceHandler`:

```csharp
options.Retry.MaxRetryAttempts = 3;
options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(12);
options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
options.CircuitBreaker.MinimumThroughput = 4;
options.CircuitBreaker.FailureRatio = 0.5;
options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(5);
```

Falha transitória do estoque é reexecutada; falha persistente abre o circuito por 5 segundos, evitando martelar um serviço caído. Esse é o motivo de esperar alguns segundos depois de restaurar o estoque na demonstração.

O gateway traduz a resposta do estoque: 409 vira `INSUFFICIENT_STOCK`, 503 e falha de rede viram `STOCK_UNAVAILABLE`, ambos com o `detail` original quando existe.

## Erros

`AppException` de domínio → middleware global → **RFC 7807 ProblemDetails**.

| Código | HTTP | Quando |
|---|---|---|
| `VALIDATION` | 400 | nota sem itens, quantidade inválida |
| `NOT_FOUND` | 404 | nota ou produto inexistente |
| `CONFLICT` | 409 | editar ou imprimir nota que não está Aberta |
| `INSUFFICIENT_STOCK` | 409 | saldo insuficiente no estoque |
| `STOCK_UNAVAILABLE` | 503 | estoque fora do ar (real ou simulado) |
| `INTERNAL` | 500 | fallback, logado e sem stack trace na resposta |

## Assistente (IA local, sem LLM)

Sem chave de API, para o avaliador rodar offline:

- **Insights**: conta notas abertas e fechadas, detecta saldo crítico e cruza a demanda das notas abertas (`SelectMany` + `GroupBy`) com o saldo do estoque para apontar risco de impressão.
- **Ask**: classifica a intenção da pergunta (notas abertas, falha, concorrência, impressão) e responde com os números reais do banco.

Se o estoque estiver fora, o assistente informa isso em vez de quebrar.

## Estrutura

```
billing-api/
├── Controllers/     InvoicesController, OpsController
├── Services/        InvoiceService (regra de negócio), AssistantService
├── Integrations/    StockGateway (HttpClient + Polly)
├── Domain/          Invoice, InvoiceItem, InvoiceSequence
├── Data/            BillingDbContext
├── Contracts/       DTOs de request e response
├── Middleware/      ExceptionHandling
└── Errors/          AppException + fábricas (BillingErrors)
```

## Configuração

```json
{
  "ConnectionStrings": {
    "Billing": "Host=localhost;Port=5433;Database=korp_billing;Username=korp;Password=korp"
  },
  "StockApi": { "BaseUrl": "http://localhost:5081" }
}
```

CORS liberado apenas para `http://localhost:4200`. Na subida roda `EnsureCreated` e cria a linha da sequência.

## Rodar

```bash
docker-compose up -d postgres
dotnet run --project microservices/stock-api  --launch-profile http   # dependência
dotnet run --project microservices/billing-api --launch-profile http
```

O estoque precisa estar no ar para criar itens e imprimir.

## Testes

```bash
dotnet test microservices/billing-api.tests
```

12 testes cobrindo numeração sequencial, criação com itens, bloqueio de edição de nota fechada, impressão fechando e baixando, reimpressão idempotente, **compensação quando o estoque falha no meio** e o `StockGateway` traduzindo 409/503 (com `HttpClient` de teste, sem rede).
