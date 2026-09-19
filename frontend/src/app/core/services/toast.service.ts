import { inject, Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject } from 'rxjs';

export interface ToastMessage {
  id: number;
  kind: 'ok' | 'error' | 'info';
  text: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private seq = 0;
  private readonly snack = inject(MatSnackBar);
  readonly messages$ = new Subject<ToastMessage>();

  success(text: string): void {
    this.push('ok', text);
  }

  error(text: string): void {
    this.push('error', text);
  }

  info(text: string): void {
    this.push('info', text);
  }

  private push(kind: ToastMessage['kind'], text: string): void {
    this.messages$.next({ id: ++this.seq, kind, text });
    const panelClass = kind === 'error' ? 'toast-error' : kind === 'ok' ? 'toast-ok' : 'toast-info';
    this.snack.open(text, 'OK', {
      duration: 4800,
      horizontalPosition: 'end',
      verticalPosition: 'bottom',
      panelClass
    });
  }
}
