import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export interface ToastMessage {
  id: number;
  kind: 'ok' | 'error' | 'info';
  text: string;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private seq = 0;
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
  }
}
