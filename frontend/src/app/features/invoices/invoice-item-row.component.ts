import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Product } from '../../core/models';

export interface DraftItem {
  productId: string;
  quantity: number;
}

@Component({
  selector: 'app-invoice-item-row',
  imports: [CommonModule, FormsModule],
  template: `
    <div class="row">
      <select [ngModel]="item.productId" (ngModelChange)="changeProduct($event)">
        <option value="">Selecione o produto</option>
        @for (product of products; track product.id) {
          <option [value]="product.id">{{ product.code }} — {{ product.description }}</option>
        }
      </select>
      <input type="number" min="1" [ngModel]="item.quantity" (ngModelChange)="changeQty($event)" />
      <div class="meta" [class.warn]="selected && item.quantity > selected.balance">
        {{ selected ? 'Saldo ' + selected.balance : '—' }}
      </div>
      <button type="button" class="link" (click)="remove.emit()">Remover</button>
    </div>
  `,
  styles: [`
    .row { display: grid; grid-template-columns: 1fr 110px 120px auto; gap: 8px; align-items: center; margin-bottom: 8px; }
    .meta { font-size: 12px; color: var(--mute); }
    .warn { color: #b42318; font-weight: 700; }
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
