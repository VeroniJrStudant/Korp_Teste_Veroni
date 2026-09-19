import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Product } from '../../core/models';

export interface DraftItem {
  productId: string;
  quantity: number;
}

@Component({
  selector: 'app-invoice-item-row',
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule
  ],
  template: `
    <div class="row">
      <mat-form-field subscriptSizing="dynamic">
        <mat-label>Produto</mat-label>
        <mat-select [ngModel]="item.productId" (ngModelChange)="changeProduct($event)">
          <mat-option value="">Selecione o produto</mat-option>
          @for (product of products; track product.id) {
            <mat-option [value]="product.id">{{ product.code }} — {{ product.description }}</mat-option>
          }
        </mat-select>
      </mat-form-field>
      <mat-form-field subscriptSizing="dynamic" class="qty">
        <mat-label>Qtd</mat-label>
        <input matInput type="number" min="1" [ngModel]="item.quantity" (ngModelChange)="changeQty($event)" />
      </mat-form-field>
      <span class="meta" [class.warn]="selected && item.quantity > selected.balance">
        {{ selected ? 'Saldo ' + selected.balance : '—' }}
      </span>
      <button mat-icon-button type="button" (click)="remove.emit()" aria-label="Remover item">
        <mat-icon>close</mat-icon>
      </button>
    </div>
  `,
  styles: [`
    .row {
      display: grid;
      grid-template-columns: minmax(0, 1fr) 112px 88px 48px;
      gap: 8px;
      align-items: center;
      margin-bottom: 8px;
    }

    .meta {
      font-size: 13px;
      color: var(--mat-sys-on-surface-variant);
      font-variant-numeric: tabular-nums;
    }

    .warn {
      color: var(--mat-sys-error);
      font-weight: 500;
    }

    @media (max-width: 720px) {
      .row {
        grid-template-columns: minmax(0, 1fr) 96px 48px;
      }

      .meta {
        grid-column: 1 / -1;
      }
    }
  `]
})
export class InvoiceItemRowComponent implements OnChanges {
  @Input({ required: true }) item!: DraftItem;
  @Input({ required: true }) products: Product[] = [];
  @Output() itemChange = new EventEmitter<DraftItem>();
  @Output() remove = new EventEmitter<void>();

  selected?: Product;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['item'] || changes['products']) {
      this.selected = this.products.find(p => p.id === this.item.productId);
    }
  }

  changeProduct(productId: string): void {
    this.itemChange.emit({ ...this.item, productId });
  }

  changeQty(quantity: number): void {
    this.itemChange.emit({ ...this.item, quantity: Number(quantity) });
  }
}
