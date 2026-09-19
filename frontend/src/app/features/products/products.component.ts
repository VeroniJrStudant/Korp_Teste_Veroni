import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { debounceTime, distinctUntilChanged, startWith, Subject, switchMap, takeUntil } from 'rxjs';
import { Product } from '../../core/models';
import { StockService } from '../../core/services/stock.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-products',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatTableModule
  ],
  templateUrl: './products.component.html',
  styleUrl: './products.component.scss'
})
export class ProductsComponent implements OnInit, AfterViewInit, OnDestroy {
  products: Product[] = [];
  readonly columns = ['code', 'description', 'balance', 'actions'];
  editing?: Product;
  suggesting = false;
  saving = false;
  private readonly destroy$ = new Subject<void>();
  private readonly fb = inject(FormBuilder);
  private readonly stock = inject(StockService);
  private readonly toast = inject(ToastService);

  readonly search = this.fb.nonNullable.control('');
  readonly form = this.fb.nonNullable.group({
    code: ['', Validators.required],
    description: ['', Validators.required],
    balance: [0, [Validators.required, Validators.min(0)]]
  });

  @ViewChild('codeInput') codeInput?: ElementRef<HTMLInputElement>;

  ngOnInit(): void {
    this.search.valueChanges.pipe(
      startWith(''),
      debounceTime(250),
      distinctUntilChanged(),
      switchMap(term => this.stock.list(term)),
      takeUntil(this.destroy$)
    ).subscribe(list => this.products = list);
  }

  ngAfterViewInit(): void {
    this.codeInput?.nativeElement.focus();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  edit(product: Product): void {
    this.editing = product;
    this.form.setValue({
      code: product.code,
      description: product.description,
      balance: product.balance
    });
    this.form.controls.code.disable();
  }

  reset(): void {
    this.editing = undefined;
    this.form.reset({ code: '', description: '', balance: 0 });
    this.form.controls.code.enable();
    this.codeInput?.nativeElement.focus();
  }

  suggest(): void {
    this.suggesting = true;
    const { code, description } = this.form.getRawValue();
    this.stock.suggestDescription(code, description).subscribe({
      next: res => {
        this.form.patchValue({ description: res.description });
        this.toast.info('Descrição gerada pelo assistente local.');
        this.suggesting = false;
      },
      error: () => this.suggesting = false
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saving = true;
    const raw = this.form.getRawValue();
    const request$ = this.editing
      ? this.stock.update(this.editing.id, { description: raw.description, balance: raw.balance })
      : this.stock.create(raw);

    request$.subscribe({
      next: () => {
        this.toast.success(this.editing ? 'Produto atualizado.' : 'Produto cadastrado.');
        this.saving = false;
        this.reset();
        this.stock.list(this.search.value).subscribe(list => this.products = list);
      },
      error: () => this.saving = false
    });
  }
}
