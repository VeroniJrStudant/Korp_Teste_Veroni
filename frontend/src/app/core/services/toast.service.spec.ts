import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ToastMessage, ToastService } from './toast.service';

describe('ToastService', () => {
  let snack: jasmine.SpyObj<MatSnackBar>;

  beforeEach(() => {
    snack = jasmine.createSpyObj<MatSnackBar>('MatSnackBar', ['open']);
    TestBed.configureTestingModule({
      providers: [{ provide: MatSnackBar, useValue: snack }]
    });
  });

  it('emite a mensagem e abre o snackbar com a classe do tipo', () => {
    const service = TestBed.inject(ToastService);
    const received: ToastMessage[] = [];
    service.messages$.subscribe(message => received.push(message));

    service.success('Produto cadastrado.');
    service.error('Estoque indisponível.');

    expect(received.map(m => m.kind)).toEqual(['ok', 'error']);
    expect(snack.open).toHaveBeenCalledTimes(2);

    const config = snack.open.calls.mostRecent().args[2];
    expect(config?.panelClass).toBe('toast-error');
  });

  it('numera as mensagens em sequência', () => {
    const service = TestBed.inject(ToastService);
    const ids: number[] = [];
    service.messages$.subscribe(message => ids.push(message.id));

    service.info('uma');
    service.info('outra');

    expect(ids).toEqual([1, 2]);
  });
});
