import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { BillingService } from './billing.service';

describe('BillingService', () => {
  let service: BillingService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(BillingService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('cria a nota com os itens informados', () => {
    service.create([{ productId: 'p1', quantity: 2 }]).subscribe();

    const req = http.expectOne('/billing-api/api/invoices');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ items: [{ productId: 'p1', quantity: 2 }] });
    req.flush({});
  });

  it('envia Idempotency-Key na impressão', () => {
    service.print('nf-1').subscribe();

    const req = http.expectOne('/billing-api/api/invoices/nf-1/print');
    expect(req.request.headers.get('Idempotency-Key')).toBe('print-nf-1');
    req.flush({});
  });
});
