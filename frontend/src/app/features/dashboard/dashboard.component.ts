import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatListModule } from '@angular/material/list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { catchError, combineLatest, of, Subscription } from 'rxjs';
import { BillingService } from '../../core/services/billing.service';
import { StockService } from '../../core/services/stock.service';
import { Insight, Invoice, Product } from '../../core/models';

@Component({
  selector: 'app-dashboard',
  imports: [
    CommonModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatListModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  products: Product[] = [];
  invoices: Invoice[] = [];
  insights: Insight[] = [];
  loading = true;
  private sub = new Subscription();

  constructor(
    private readonly stock: StockService,
    private readonly billing: BillingService
  ) {}

  ngOnInit(): void {
    this.sub.add(
      combineLatest({
        products: this.stock.list().pipe(catchError(() => of([]))),
        invoices: this.billing.list().pipe(catchError(() => of([]))),
        insights: this.billing.insights().pipe(catchError(() => of([])))
      }).subscribe(data => {
        this.products = data.products;
        this.invoices = data.invoices;
        this.insights = data.insights;
        this.loading = false;
      })
    );
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  get openCount(): number {
    return this.invoices.filter(i => i.status === 'Aberta').length;
  }

  get closedCount(): number {
    return this.invoices.filter(i => i.status === 'Fechada').length;
  }

  get stockTotal(): number {
    return this.products.reduce((acc, p) => acc + p.balance, 0);
  }

  icon(severity: Insight['severity']): string {
    if (severity === 'danger') return 'error';
    if (severity === 'warning') return 'warning';
    if (severity === 'ok') return 'check_circle';
    return 'info';
  }
}
