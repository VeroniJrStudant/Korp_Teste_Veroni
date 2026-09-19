import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Product } from '../../core/models';
import { InvoiceItemRowComponent } from './invoice-item-row.component';

describe('InvoiceItemRowComponent', () => {
  const products: Product[] = [
    {
      id: 'p1',
      code: 'PAR-M8',
      description: 'Parafuso sextavado M8',
      balance: 1,
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-01T00:00:00Z'
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvoiceItemRowComponent],
      providers: [provideNoopAnimations()]
    }).compileComponents();
  });

  it('resolve o produto selecionado no ngOnChanges', () => {
    const fixture = TestBed.createComponent(InvoiceItemRowComponent);
    fixture.componentRef.setInput('products', products);
    fixture.componentRef.setInput('item', { productId: 'p1', quantity: 1 });
    fixture.detectChanges();

    expect(fixture.componentInstance.selected?.code).toBe('PAR-M8');
  });

  it('emite a quantidade como número', () => {
    const fixture = TestBed.createComponent(InvoiceItemRowComponent);
    fixture.componentRef.setInput('products', products);
    fixture.componentRef.setInput('item', { productId: 'p1', quantity: 1 });
    fixture.detectChanges();

    const component = fixture.componentInstance;
    let emitted: { productId: string; quantity: number } | undefined;
    component.itemChange.subscribe(value => emitted = value);

    component.changeQty('3' as unknown as number);

    expect(emitted).toEqual({ productId: 'p1', quantity: 3 });
  });
});
