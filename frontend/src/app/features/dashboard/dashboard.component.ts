import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { catchError, combineLatest, of, Subscription } from 'rxjs';
import { BillingService } from '../../core/billing.service';
import { StockService } from '../../core/stock.service';
import { Insight, Invoice, Product } from '../../core/models';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, RouterLink],
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
}
