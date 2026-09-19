# Detalhamento técnico — Sistema de emissão de Notas Fiscais

Candidata: Veroni
Stack: Angular 19 + ASP.NET Core / C# (.NET 10) + PostgreSQL
Arquitetura: dois microsserviços (Estoque e Faturamento)

Este arquivo responde, na ordem do enunciado, o que o PDF pede no detalhamento técnico.

## 1. Visão da solução

O frontend Angular consome duas APIs REST independentes, cada uma com o próprio banco:

- **Korp.Stock.Api** (`:5081`): cadastro de produtos, consulta de saldo, baixa/estorno com lock de concorrência e modo de falha simulada.
- **Korp.Billing.Api** (`:5082`): numeração sequencial de notas, itens, impressão, compensação e assistente.

Pastas: `frontend/`, `microservices/stock-api`, `microservices/billing-api`, `infra/`, `scripts/`.

A baixa de estoque **não** ocorre no cadastro da nota. Ocorre só na **impressão**, como pede o enunciado. Se o estoque falhar no meio da impressão, o faturamento estorna as baixas já feitas e a nota permanece **Aberta**.

```
[Angular :4200]
    |  REST
    |----------------------\
    v                       v
Estoque API            Faturamento API
(PostgreSQL            (PostgreSQL
 korp_stock)            korp_billing)
                           |
                           | HTTP + Polly
                           v
                      Estoque API
```

## 2. Quais ciclos de vida do Angular foram utilizados

| Hook | Onde | Para quê |
|---|---|---|
| `ngOnInit` | Shell, Painel, Produtos, Notas, editor | Cargas HTTP, busca com debounce, health checks e breakpoint do drawer. |
| `ngAfterViewInit` | Produtos e editor da nota | Foco no primeiro campo ou no botão de adicionar item. |
| `ngOnChanges` | `InvoiceItemRowComponent` | Quando o produto do item muda, atualiza o saldo visível na linha. |
| `ngOnDestroy` | Shell, Painel, Produtos, editor | Cancela `Subscription` / `takeUntil` para não vazar memória nem HTTP. |

Os componentes são **standalone**. As rotas de tela usam `loadComponent` (lazy loading).

## 3. Se foi feito uso da biblioteca RxJS e, em caso afirmativo, como

Sim. O RxJS é o modelo de assincronismo do frontend.

- `switchMap` + `debounceTime` + `distinctUntilChanged`: busca de produtos (cancela a request anterior enquanto a pessoa digita).
- `interval` + `startWith` + `switchMap` + `catchError`: *polling* de saúde das APIs a cada 5 s, sem quebrar a UI se uma delas cair.
- `combineLatest`: o Painel junta produtos, notas e insights numa única atualização.
- `exhaustMap`: o botão Imprimir ignora cliques repetidos enquanto a request está em voo.
- `finalize`: encerra o indicador de processamento da impressão.
- `Subject` / `messages$`: canal interno de mensagens do `ToastService` (exibidas em `MatSnackBar`).
- `takeUntil`: descadastro no `ngOnDestroy`.
- `BreakpointObserver` (CDK, também Observable): alterna o drawer entre `side` e `over`.
- Interceptor HTTP: `catchError` lê `ProblemDetails` do backend e mostra o `detail`.

## 4. Quais outras bibliotecas foram utilizadas e para qual finalidade

Além do Angular 19 (Common, Forms, Router, HttpClient, Animations) e RxJS 7.8:

- **Angular Material 19** + **CDK**: UI completa em Material Design 3, incluindo layout, tema e breakpoints.
- **Roboto** e **Material Icons** (Google Fonts): tipografia e ícones oficiais do Material.
- No backend, via NuGet: EF Core + Npgsql, Swashbuckle, `Microsoft.Extensions.Http.Resilience`.
- Nos testes: xUnit, Testcontainers (PostgreSQL), Jasmine/Karma no Angular.

Não há biblioteca visual paralela (Bootstrap, Tailwind, CSS de ERP próprio). O `styles.scss` aplica `mat.theme` e alguns tokens globais.

## 5. Para componentes visuais, quais bibliotecas foram utilizadas

**Angular Material 19**, alinhado ao Material Design 3:

- `MatSidenav` + `MatToolbar` + `MatNavList` + `MatIcon` — casco da aplicação.
- `MatCard` — KPIs, listagens, formulários e preview da nota.
- `MatButton` — cadastrar, imprimir, simular falha, perguntar.
- `MatFormField` + `MatInput` + `MatSelect` — produto, busca, itens da nota e assistente.
- `MatTable` — estoque e notas fiscais.
- `MatChip` — status **Aberta** / **Fechada** e saúde das APIs.
- `MatList` — insights do Painel.
- `MatSnackBar` — feedback de erro e sucesso (via interceptor e serviços).
- `MatProgressSpinner` — carga do Painel e overlay da impressão.
- `MatSlideToggle` + `MatTooltip` — troca de tema claro/escuro na toolbar.

Decisões de UX/UI: escala de 8 px em espaçamentos, drawer de 280 px em `surface` com divisória de 1 px, item ativo em pill, toolbar sticky de 64 px, títulos e rotas com transição de 200 ms respeitando `prefers-reduced-motion`.

**Tema claro e escuro:** `mat.theme` com `theme-type: color-scheme` emite valores `light-dark()`. O `ThemeService` (signal) aplica a classe `theme-dark` no `html`, persiste em `localStorage` e cai no `prefers-color-scheme` na primeira visita. Um script inline no `index.html` evita o flash de tema errado.

O documento impresso (DANFE de demonstração, **não** é NF-e SEFAZ) é HTML dentro de um `mat-card`, com CSS de `@media print`. Não existe componente Material de nota fiscal.

## 6. Como foi realizado o gerenciamento de dependências no Golang (se aplicável)

**Não aplicável.** O backend é **C# / .NET 10**. As dependências entram via **NuGet** nos `.csproj` e são restauradas com `dotnet restore`. Não há `go.mod`.

## 7. Quais frameworks foram utilizados no Golang ou C#

- ASP.NET Core Web API (controllers + Swagger/Swashbuckle)
- Entity Framework Core 10 + provedor **Npgsql** (PostgreSQL)
- `Microsoft.Extensions.Http.Resilience` (stack **Polly**: retry, timeout e circuit breaker) **somente** no `HttpClient` do faturamento para o estoque
- CORS restrito a `http://localhost:4200`

LINQ é `System.Linq` do .NET (métodos em coleções e `IQueryable`), não um framework separado.

## 8. Como foram tratados os erros e exceções no backend

Padrão único nos dois serviços:

1. Exceções de domínio (`AppException`) com código, título, detalhe e HTTP status.
2. Middleware global converte qualquer exceção em **RFC 7807 ProblemDetails** (`title`, `detail`, `status`, `code`).
3. Casos previstos:
   - `VALIDATION` 400 — quantidade/código inválidos
   - `NOT_FOUND` 404
   - `INSUFFICIENT_STOCK` / `CONFLICT` 409 — saldo insuficiente ou nota que não está Aberta
   - `STOCK_UNAVAILABLE` 503 — estoque fora (falha real ou simulada)
   - `INTERNAL` 500 — fallback logado, sem vazar stack para o usuário
4. Na impressão, falha no estoque dispara **compensação** (crédito das baixas já aplicadas) e a nota **não fecha**.
5. O frontend mostra o `detail` no snackbar. O usuário consegue tentar de novo.

Falha simulada: `POST /api/chaos` no estoque. O middleware passa a responder 503 nas rotas de negócio. `/health` e `/chaos` continuam no ar para o Painel e para a restauração.

## 9. Caso a implementação utilize C#, indicar se foi utilizado LINQ e de que forma

Sim. Exemplos no código:

- Filtro e ordenação de produtos: `Where` + `OrderBy` + `Select` + `ToListAsync` (`ProductService`).
- Listagem de notas: `OrderByDescending` + `Select` (`InvoiceService`).
- Itens da nota agrupados por produto: `GroupBy` + `Sum` das quantidades.
- Insights: `Where` (abertas/fechadas), `SelectMany` dos itens, `GroupBy` da demanda e cruzamento com o saldo do estoque para achar risco de impressão (`AssistantService`).
- Compensação: `Enumerable.Reverse` das baixas parciais.

A numeração sequencial **não** usa `Max + 1`. Há incremento atômico da tabela `invoice_sequences` dentro de transação.

## 10. Requisitos opcionais

### a) Concorrência

No débito, o estoque abre transação e usa `pg_advisory_xact_lock(productId)`. Duas impressões no produto `PAR-M8` (saldo 1) não baixam o mesmo item duas vezes: uma nota fecha, a outra recebe 409 e permanece Aberta.

Script: `scripts/test-concurrency.sh`.

### b) Inteligência artificial

Sem chave de LLM, para o avaliador rodar offline:

- `POST /api/ai/suggest-description` no estoque: gera descrição comercial a partir do código/nome.
- `GET /api/ai/insights` no faturamento: fila, saldo crítico e risco de impressão (Painel).
- `POST /api/ai/ask`: assistente por intenções (notas abertas, falha, concorrência) com dados reais.

### c) Idempotência

- Impressão de nota já **Fechada** devolve sucesso sem nova baixa.
- Cada baixa leva `operationId = invoice:{id}:product:{id}`. Replay não desconta de novo.
- Header `Idempotency-Key` na impressão.
- `exhaustMap` no botão evita double-submit no browser.

## 11. Banco de dados e testes

PostgreSQL 16 (Docker, porta 5433). Bancos `korp_stock` e `korp_billing`. As APIs executam `EnsureCreated` + seed na subida.

Os testes de integração usam o **mesmo PostgreSQL** (Testcontainers ou o compose na 5433). Sem Docker, esses testes são ignorados; os de IA local, gateway HTTP e Angular continuam.

```bash
dotnet test Korp.Teste.slnx
cd frontend && npm test
```

No Angular: serviços HTTP, interceptor de erro, `ngOnChanges` da linha de item, número sequencial da lista e o serviço de tema (persistência e classe no `html`).

## 12. Como rodar a demonstração de falha

1. Crie uma nota Aberta.
2. No drawer, **Simular falha do estoque** (chip de Estoque fica vermelho).
3. Clique em **Imprimir nota fiscal**: spinner, depois snackbar de indisponibilidade; status continua Aberta.
4. **Restaurar estoque**, aguarde cerca de 5 segundos (circuit breaker do Polly) e imprima de novo: a nota fecha e o saldo baixa.
