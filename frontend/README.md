# Frontend — Korp Faturamento

Interface do sistema de emissão de notas fiscais.

**Angular 19 standalone · Angular Material 19 (Material Design 3) · RxJS 7.8 · porta 4200**

Consome dois backends independentes: estoque (`:5081`) e faturamento (`:5082`).

## Rodar

```bash
npm install
npm start          # http://localhost:4200
```

As APIs precisam estar no ar. O `proxy.conf.json` evita CORS no desenvolvimento:

| Caminho no Angular | Destino real |
|---|---|
| `/stock-api/api/...` | `http://localhost:5081/api/...` |
| `/billing-api/api/...` | `http://localhost:5082/api/...` |

```bash
npm run build      # bundle de produção em dist/
npm test           # Jasmine + Karma em ChromeHeadless
```

## Estrutura

```
src/app/
├── core/
│   ├── models/          Product, Invoice, Insight, ApiProblem…
│   ├── services/        stock, billing, toast, theme
│   └── interceptors/    error.interceptor
├── layout/
│   └── shell.component  drawer + app bar + router-outlet
├── features/
│   ├── dashboard/       KPIs e insights
│   ├── products/        cadastro e estoque
│   ├── invoices/        lista, editor da nota e linha de item
│   └── assistant/       perguntas de domínio
├── app.routes.ts        rotas com loadComponent (lazy)
└── app.config.ts        router, HttpClient + interceptor, animations
```

`core` é infraestrutura, `layout` é o casco, `features` são as telas. Cada tela entra por *lazy loading*.

## Telas

| Rota | Tela | O que faz |
|---|---|---|
| `/` | Painel | KPIs, insights do assistente e roteiro da demonstração |
| `/produtos` | Produtos | Cadastro/edição e tabela de estoque com busca |
| `/notas` | Notas fiscais | Lista com número, status e data |
| `/notas/nova` | Editor | Monta os itens e cria a nota Aberta |
| `/notas/:id` | Editor | Edita itens (se Aberta) e **imprime** |
| `/assistente` | Assistente | Pergunta em linguagem natural sobre o domínio |

O drawer ainda mostra o status das duas APIs e o botão **Simular falha do estoque**.

## Ciclos de vida do Angular

| Hook | Onde | Para quê |
|---|---|---|
| `ngOnInit` | Shell, Painel, Produtos, Notas, Editor | Carregar dados, iniciar polling de saúde e observar breakpoint |
| `ngAfterViewInit` | Produtos, Editor | Foco no primeiro campo via `@ViewChild` |
| `ngOnChanges` | `InvoiceItemRowComponent` | Recalcular o saldo exibido quando o produto do item muda |
| `ngOnDestroy` | Shell, Painel, Produtos, Editor | Encerrar inscrições (`takeUntil` ou `Subscription.unsubscribe`) |

## RxJS

| Operador | Onde | Por quê |
|---|---|---|
| `debounceTime` + `distinctUntilChanged` + `switchMap` | busca de produtos | não dispara HTTP a cada tecla e cancela a busca anterior |
| `combineLatest` | Painel | só renderiza quando produtos, notas e insights chegam |
| `interval` + `startWith` + `catchError` | Shell | *health check* a cada 5 s sem quebrar a tela se uma API cair |
| `exhaustMap` | botão Imprimir | ignora clique repetido enquanto a impressão está em voo |
| `finalize` | impressão | ajusta a mensagem do overlay ao terminar |
| `takeUntil(destroy$)` | telas | evita vazamento de inscrição |
| `catchError` | interceptor | traduz `ProblemDetails` em mensagem para o usuário |

`exhaustMap` e não `switchMap` no Imprimir: cancelar uma impressão em andamento poderia deixar baixa de estoque sem nota fechada.

## Tratamento de erro

O `errorInterceptor` intercepta toda resposta com falha, lê `detail` (ou `title`) do **ProblemDetails** e mostra no `MatSnackBar`. Chamadas de `/health` são silenciosas — senão o polling encheria a tela de aviso a cada 5 segundos.

## Design system

Tudo é Angular Material, sem kit visual paralelo:

`MatSidenav` · `MatToolbar` · `MatNavList` · `MatIcon` · `MatCard` · `MatButton` · `MatFormField` · `MatInput` · `MatSelect` · `MatTable` · `MatChip` · `MatList` · `MatSnackBar` · `MatProgressSpinner` · `MatSlideToggle` · `MatTooltip`

Decisões de UX/UI:

- **Escala de 8**: espaçamentos em múltiplos de 8 px; app bar de 64; drawer de 280.
- **Drawer** em `surface` com divisória de 1 px, item ativo em pill, marca com bloco de 40 px.
- **App bar** sticky, com título da rota, aviso de baixa de estoque e o switch de tema.
- **Responsivo** via `BreakpointObserver`: abaixo de 960 px o drawer vira `over` e fecha ao navegar.
- **Movimento** de 200 ms com `ease-out`, respeitando `prefers-reduced-motion`.
- **Acessibilidade**: skip link, `main` como landmark, `aria-label` nos botões de ícone, foco visível, navegação por teclado.
- **Números** com `font-variant-numeric: tabular-nums`, alinhados à direita.

### Tema claro e escuro

`styles.scss` aplica `mat.theme` com `theme-type: color-scheme`, que emite valores `light-dark()`. Trocar de tema é trocar a propriedade CSS `color-scheme`:

```scss
html { color-scheme: light; }
html.theme-dark { color-scheme: dark; }
```

O `ThemeService` (signal) aplica a classe no `<html>`, salva em `localStorage` e, na primeira visita, segue `prefers-color-scheme`. Um script inline no `index.html` aplica a classe antes da primeira pintura, evitando o flash de tema errado.

Nenhum componente tem cor fixa em hexadecimal: tudo sai de `--mat-sys-*`, então o modo escuro funciona sem CSS duplicado.

## Impressão da nota

O preview (DANFE de demonstração, **não** é NF-e SEFAZ) é HTML dentro de um `mat-card`. O `@media print` esconde drawer, app bar e o formulário, deixando só o documento. Não existe componente Material para nota fiscal.

## Testes

```bash
npm test
```

16 specs:

- **Serviços HTTP** (`HttpTestingController`): parâmetro de busca, corpo do POST, header `Idempotency-Key`.
- **Interceptor**: mostra o `detail` do ProblemDetails; ignora falha de `/health`.
- **ToastService**: classe do snackbar por tipo e numeração das mensagens.
- **ThemeService**: aplica a classe no `html` e persiste a escolha.
- **Componentes**: `ngOnInit` da lista de notas, formatação do número sequencial e `ngOnChanges` da linha de item.
