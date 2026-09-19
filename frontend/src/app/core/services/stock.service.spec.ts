import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Product } from '../models';
import { StockService } from './stock.service';

describe('StockService', () => {
  let service: StockService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(StockService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lista produtos sem filtro quando a busca está vazia', () => {
    const products: Product[] = [];
    service.list().subscribe(list => expect(list).toEqual(products));

    const req = http.expectOne('/stock-api/api/products');
    expect(req.request.method).toBe('GET');
    req.flush(products);
  });

  it('envia o termo de busca como parâmetro', () => {
    service.list(' PAR-M8 ').subscribe();

    const req = http.expectOne(r => r.url === '/stock-api/api/products');
    expect(req.request.params.get('search')).toBe('PAR-M8');
    req.flush([]);
  });

  it('alterna o modo de falha simulada', () => {
    service.chaos(true).subscribe();

    const req = http.expectOne('/stock-api/api/chaos');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ enabled: true });
    req.flush({ enabled: true, message: 'Falha simulada ativada.' });
  });
});
