import { TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { Invoice } from '../../core/models';
import { BillingService } from '../../core/services/billing.service';
import { InvoicesComponent } from './invoices.component';

describe('InvoicesComponent', () => {
  const invoices: Invoice[] = [
    {
      id: 'nf-1',
      number: 12,
      status: 'Aberta',
      createdAt: '2026-01-01T00:00:00Z',
      closedAt: null,
      items: []
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvoicesComponent],
      providers: [
        provideRouter([]),
        provideNoopAnimations(),
        { provide: BillingService, useValue: { list: () => of(invoices) } }
      ]
    }).compileComponents();
  });

  it('carrega a lista no ngOnInit e formata o número sequencial', () => {
    const fixture = TestBed.createComponent(InvoicesComponent);
    fixture.detectChanges();
    const component = fixture.componentInstance;

    expect(component.invoices.length).toBe(1);
    expect(component.pad(12)).toBe('00012');
  });
});
