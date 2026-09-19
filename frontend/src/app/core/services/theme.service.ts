import { DOCUMENT } from '@angular/common';
import { inject, Injectable, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';

const STORAGE_KEY = 'korp.theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  readonly dark = signal(false);

  constructor() {
    this.apply(this.restore());
  }

  toggle(): void {
    this.setDark(!this.dark());
  }

  setDark(dark: boolean): void {
    this.apply(dark ? 'dark' : 'light');
  }

  private restore(): ThemeMode {
    const saved = this.read();
    if (saved) {
      return saved;
    }
    const prefersDark = this.document.defaultView?.matchMedia('(prefers-color-scheme: dark)').matches;
    return prefersDark ? 'dark' : 'light';
  }

  private apply(mode: ThemeMode): void {
    this.dark.set(mode === 'dark');
    this.document.documentElement.classList.toggle('theme-dark', mode === 'dark');
    try {
      this.document.defaultView?.localStorage.setItem(STORAGE_KEY, mode);
    } catch {
      // modo privado: mantém só a preferência da sessão
    }
  }

  private read(): ThemeMode | null {
    try {
      const saved = this.document.defaultView?.localStorage.getItem(STORAGE_KEY);
      return saved === 'light' || saved === 'dark' ? saved : null;
    } catch {
      return null;
    }
  }
}
