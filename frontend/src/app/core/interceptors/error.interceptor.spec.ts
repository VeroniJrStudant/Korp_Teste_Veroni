import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ToastService } from '../services/toast.service';
import { errorInterceptor } from './error.interceptor';

describe('errorInterceptor', () => {
  let http: HttpClient;
  let controller: HttpTestingController;
  let toast: jasmine.SpyObj<ToastService>;

  beforeEach(() => {
    toast = jasmine.createSpyObj<ToastService>('ToastService', ['error']);
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        provideHttpClientTesting(),
        { provide: ToastService, useValue: toast }
      ]
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('mostra o detail do ProblemDetails quando a requisição falha', () => {
    http.get('/billing-api/api/invoices').subscribe({ error: () => undefined });

    controller.expectOne('/billing-api/api/invoices').flush(
      { detail: 'Estoque indisponível.', status: 503 },
      { status: 503, statusText: 'Service Unavailable' }
    );

    expect(toast.error).toHaveBeenCalledWith('Estoque indisponível.');
  });

  it('não notifica falhas de health check', () => {
    http.get('/stock-api/api/health').subscribe({ error: () => undefined });

    controller.expectOne('/stock-api/api/health').flush(
      {},
      { status: 503, statusText: 'Service Unavailable' }
    );

    expect(toast.error).not.toHaveBeenCalled();
  });
});
