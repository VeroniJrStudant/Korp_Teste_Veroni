import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { Invoice } from '../../core/models';
import { BillingService } from '../../core/services/billing.service';

@Component({
  selector: 'app-invoices',
  imports: [
    CommonModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatTableModule
  ],
  templateUrl: './invoices.component.html',
  styleUrl: './invoices.component.scss'
})
export class InvoicesComponent implements OnInit, OnDestroy {
  invoices: Invoice[] = [];
  readonly columns = ['number', 'status', 'items', 'createdAt', 'actions'];
  private sub = new Subscription();

  constructor(private readonly billing: BillingService) {}

  ngOnInit(): void {
    this.sub.add(this.billing.list().subscribe(list => this.invoices = list));
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  pad(n: number): string {
    return n.toString().padStart(5, '0');
  }
}
