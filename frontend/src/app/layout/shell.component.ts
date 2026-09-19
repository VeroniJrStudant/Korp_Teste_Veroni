import { animate, style, transition, trigger } from '@angular/animations';
import { BreakpointObserver } from '@angular/cdk/layout';
import { CommonModule } from '@angular/common';
import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { catchError, filter, interval, map, of, startWith, Subscription, switchMap } from 'rxjs';
import { BillingService } from '../core/services/billing.service';
import { StockService } from '../core/services/stock.service';
import { ThemeService } from '../core/services/theme.service';
import { ToastService } from '../core/services/toast.service';

const fade = [
  style({ opacity: 0, transform: 'translateY(8px)' }),
  animate('200ms ease-out', style({ opacity: 1, transform: 'none' }))
];

@Component({
  selector: 'app-shell',
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatButtonModule,
    MatChipsModule,
    MatIconModule,
    MatListModule,
    MatSidenavModule,
    MatSlideToggleModule,
    MatToolbarModule,
    MatTooltipModule
  ],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  animations: [
    trigger('titleFade', [transition('* => *', fade)]),
    trigger('routeFade', [transition('* => *', fade)])
  ]
})
export class ShellComponent implements OnInit, OnDestroy {
  readonly theme = inject(ThemeService);
  readonly hint = 'O saldo do estoque só é baixado no botão Imprimir nota.';

  stockOk = false;
  billingOk = false;
  chaos = false;
  toggling = false;
  compact = false;
  navOpened = true;
  pageTitle = 'Painel';

  private sub = new Subscription();

  constructor(
    private readonly stock: StockService,
    private readonly billing: BillingService,
    private readonly toast: ToastService,
    private readonly router: Router,
    private readonly breakpoints: BreakpointObserver
  ) {}

  ngOnInit(): void {
    this.sub.add(
      this.breakpoints.observe('(max-width: 960px)').subscribe(state => {
        this.compact = state.matches;
        this.navOpened = !state.matches;
      })
    );

    this.sub.add(
      interval(5000).pipe(
        startWith(0),
        switchMap(() => this.stock.health().pipe(
          catchError(() => of({ service: 'stock', status: 'down', chaos: false }))
        ))
      ).subscribe(h => {
        this.stockOk = h.status === 'healthy';
        this.chaos = !!h.chaos;
      })
    );

    this.sub.add(
      interval(5000).pipe(
        startWith(0),
        switchMap(() => this.billing.health().pipe(
          catchError(() => of({ service: 'billing', status: 'down' }))
        ))
      ).subscribe(h => this.billingOk = h.status === 'healthy')
    );

    this.sub.add(
      this.router.events.pipe(
        filter((e): e is NavigationEnd => e instanceof NavigationEnd),
        map(e => e.urlAfterRedirects)
      ).subscribe(url => this.setTitle(url))
    );
    this.setTitle(this.router.url);
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  toggleNav(): void {
    this.navOpened = !this.navOpened;
  }

  closeNavOnNavigate(): void {
    if (this.compact) {
      this.navOpened = false;
    }
  }

  toggleChaos(): void {
    this.toggling = true;
    this.stock.chaos(!this.chaos).subscribe({
      next: res => {
        this.chaos = res.enabled;
        this.stockOk = !res.enabled;
        this.toast.info(res.message);
        this.toggling = false;
      },
      error: () => this.toggling = false
    });
  }

  private setTitle(url: string): void {
    if (url.startsWith('/produtos')) this.pageTitle = 'Produtos';
    else if (url.startsWith('/notas')) this.pageTitle = 'Notas fiscais';
    else if (url.startsWith('/assistente')) this.pageTitle = 'Assistente';
    else this.pageTitle = 'Painel';
  }
}
