import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { catchError, EMPTY, exhaustMap, finalize, Subject, takeUntil } from 'rxjs';
import { Invoice, Product } from '../../core/models';
import { BillingService } from '../../core/services/billing.service';
import { StockService } from '../../core/services/stock.service';
import { ToastService } from '../../core/services/toast.service';
import { DraftItem, InvoiceItemRowComponent } from './invoice-item-row.component';

@Component({
  selector: 'app-invoice-editor',
  imports: [
    CommonModule,
    RouterLink,
    InvoiceItemRowComponent,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './invoice-editor.component.html',
  styleUrl: './invoice-editor.component.scss'
})
export class InvoiceEditorComponent implements OnInit, AfterViewInit, OnDestroy {
  products: Product[] = [];
  items: DraftItem[] = [{ productId: '', quantity: 1 }];
  invoice?: Invoice;
  saving = false;
  printing = false;
  printMessage = '';
  showPreview = false;
  private readonly destroy$ = new Subject<void>();
  private readonly print$ = new Subject<void>();

  @ViewChild('addBtn') addBtn?: ElementRef<HTMLButtonElement>;

  constructor(
    private readonly stock: StockService,
    private readonly billing: BillingService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly toast: ToastService
  ) {}

  ngOnInit(): void {
    this.stock.list().subscribe(list => this.products = list);

    this.route.paramMap.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const id = params.get('id');
      if (!id) {
        this.invoice = undefined;
        this.items = [{ productId: '', quantity: 1 }];
        return;
      }
      this.billing.get(id).subscribe(invoice => {
        this.invoice = invoice;
        this.items = invoice.items.map(i => ({ productId: i.productId, quantity: i.quantity }));
      });
    });

    this.print$.pipe(
      exhaustMap(() => {
        if (!this.invoice) {
          this.toast.error('Salve a nota antes de imprimir.');
          return EMPTY;
        }
        if (this.invoice.status === 'Fechada') {
          this.showPreview = true;
          this.toast.info('Nota já Fechada. Reimpressão do documento, sem nova baixa de estoque.');
          return EMPTY;
        }
        this.printing = true;
        this.printMessage = 'Processando impressão e baixa de estoque…';
        return this.billing.print(this.invoice.id).pipe(
          catchError(() => {
            this.printing = false;
            this.printMessage = '';
            return EMPTY;
          }),
          finalize(() => {
            if (this.printing) {
              this.printMessage = 'Finalizando documento…';
            }
          }),
          takeUntil(this.destroy$)
        );
      }),
      takeUntil(this.destroy$)
    ).subscribe(result => {
      this.invoice = result.invoice;
      this.printing = false;
      this.printMessage = '';
      this.showPreview = true;
      this.toast.success(result.message);
      this.stock.list().subscribe(list => this.products = list);
    });
  }

  ngAfterViewInit(): void {
    this.addBtn?.nativeElement.focus();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get canEdit(): boolean {
    return !this.invoice || this.invoice.status === 'Aberta';
  }

  addItem(): void {
    this.items = [...this.items, { productId: '', quantity: 1 }];
  }

  updateItem(index: number, item: DraftItem): void {
    this.items = this.items.map((current, i) => i === index ? item : current);
  }

  removeItem(index: number): void {
    this.items = this.items.filter((_, i) => i !== index);
  }

  save(): void {
    const payload = this.items.filter(i => i.productId && i.quantity > 0);
    if (!payload.length) {
      this.toast.error('Inclua ao menos um produto.');
      return;
    }
    this.saving = true;
    const request$ = this.invoice
      ? this.billing.update(this.invoice.id, payload)
      : this.billing.create(payload);

    request$.subscribe({
      next: invoice => {
        this.saving = false;
        this.toast.success(this.invoice ? 'Nota atualizada.' : `Nota ${this.pad(invoice.number)} criada como Aberta.`);
        void this.router.navigate(['/notas', invoice.id]);
        this.invoice = invoice;
      },
      error: () => this.saving = false
    });
  }

  print(): void {
    this.print$.next();
  }

  printPaper(): void {
    window.print();
  }

  pad(n: number): string {
    return n.toString().padStart(5, '0');
  }
}
