import { Component, OnDestroy, OnInit } from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { catchError, filter, interval, map, of, startWith, Subscription, switchMap } from 'rxjs';
import { BillingService } from '../core/billing.service';
import { StockService } from '../core/stock.service';
import { ToastMessage, ToastService } from '../core/toast.service';

@Component({
  selector: 'app-shell',
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss'
})
export class ShellComponent implements OnInit, OnDestroy {
  stockOk = false;
  billingOk = false;
  chaos = false;
  toggling = false;
  toasts: ToastMessage[] = [];
  pageTitle = 'Painel';

  private sub = new Subscription();

  constructor(
    private readonly stock: StockService,
    private readonly billing: BillingService,
    private readonly toast: ToastService,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
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

    this.sub.add(this.toast.messages$.subscribe(message => {
      this.toasts = [...this.toasts, message];
      setTimeout(() => this.toasts = this.toasts.filter(t => t.id !== message.id), 4200);
    }));

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
