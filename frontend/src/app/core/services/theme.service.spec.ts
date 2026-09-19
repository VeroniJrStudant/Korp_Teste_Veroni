import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  beforeEach(() => {
    localStorage.removeItem('korp.theme');
    document.documentElement.classList.remove('theme-dark');
    TestBed.configureTestingModule({});
  });

  afterEach(() => {
    localStorage.removeItem('korp.theme');
    document.documentElement.classList.remove('theme-dark');
  });

  it('aplica a classe de tema escuro e persiste a escolha', () => {
    const service = TestBed.inject(ThemeService);

    service.setDark(true);

    expect(service.dark()).toBeTrue();
    expect(document.documentElement.classList.contains('theme-dark')).toBeTrue();
    expect(localStorage.getItem('korp.theme')).toBe('dark');
  });

  it('volta para o tema claro no toggle', () => {
    const service = TestBed.inject(ThemeService);
    service.setDark(true);

    service.toggle();

    expect(service.dark()).toBeFalse();
    expect(document.documentElement.classList.contains('theme-dark')).toBeFalse();
    expect(localStorage.getItem('korp.theme')).toBe('light');
  });
});
