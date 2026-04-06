import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { TransactionService } from '../../../core/services/transaction.service';
import { ProductService } from '../../../core/services/product.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Product } from '../../../core/models/product.model';
import { Transaction } from '../../../core/models/transaction.model';
import { pageEnter, scaleIn } from '../../../core/animations/app.animations';

@Component({
  selector: 'app-transaction-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CurrencyPipe, ReactiveFormsModule, MatFormFieldModule, MatInputModule,
    MatSelectModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatDatepickerModule, MatNativeDateModule
  ],
  animations: [pageEnter, scaleIn],
  templateUrl: './transaction-form.component.html',
  styleUrl: './transaction-form.component.scss'
})
export class TransactionFormComponent implements OnInit {
  private readonly transactionService = inject(TransactionService);
  private readonly productService     = inject(ProductService);
  private readonly notify             = inject(NotificationService);
  private readonly route              = inject(ActivatedRoute);
  readonly router                     = inject(Router);

  readonly isEdit          = signal(false);
  readonly saving          = signal(false);
  readonly loadingData     = signal(false);
  readonly products        = signal<Product[]>([]);
  readonly selectedProduct = signal<Product | null>(null);
  private transactionId: string | null = null;

  readonly form = inject(FormBuilder).group({
    type:      ['', Validators.required],
    productId: ['', Validators.required],
    quantity:  [1, [Validators.required, Validators.min(1), Validators.max(9999), isInteger]],
    unitPrice: [0, [Validators.required, Validators.min(0), Validators.max(999999.99), maxDecimals(2)]],
    date:      [null as Date | null],
    detail:    ['', Validators.maxLength(500)]
  });

  readonly totalCalculado = computed(() => {
    const qty   = +(this.form.get('quantity')?.value  ?? 0);
    const price = +(this.form.get('unitPrice')?.value ?? 0);
    return qty * price;
  });

  ngOnInit(): void {
    this.transactionId = this.route.snapshot.paramMap.get('id');
    if (this.transactionId) {
      this.isEdit.set(true);
      this.form.get('type')?.clearValidators();
      this.form.get('productId')?.clearValidators();
      this.form.get('quantity')?.clearValidators();
      this.form.get('unitPrice')?.clearValidators();
      this.form.updateValueAndValidity();
      this.loadTransaction(this.transactionId);
    } else {
      this.loadProducts();
    }
  }

  private loadProducts(): void {
    this.productService.getAll({ pageSize: 100 }).subscribe({
      next: res => this.products.set(res.data)
    });
  }

  private loadTransaction(id: string): void {
    this.loadingData.set(true);
    this.transactionService.getById(id).subscribe({
      next: (t: Transaction) => {
        this.form.patchValue({ detail: t.detail ?? '', date: t.date ? new Date(t.date) : null });
        this.loadingData.set(false);
      },
      error: () => { this.loadingData.set(false); this.router.navigate(['/transactions']); }
    });
  }

  onProductChange(): void {
    const pid = this.form.get('productId')?.value;
    const p   = this.products().find(x => x.id === pid) ?? null;
    this.selectedProduct.set(p);
    this.form.get('quantity')?.setValidators([
      Validators.required, Validators.min(1), this.stockValidator.bind(this)
    ]);
    this.form.get('quantity')?.updateValueAndValidity();
  }

  private stockValidator(control: AbstractControl): ValidationErrors | null {
    const type    = this.form?.get('type')?.value;
    const product = this.selectedProduct();
    if (type === 'Sale' && product && control.value > product.stock) {
      return { stockExceeded: true };
    }
    return null;
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);

    if (this.isEdit()) {
      const payload = {
        detail: this.form.value.detail || undefined,
        date: this.form.value.date ? new Date(this.form.value.date!).toISOString() : undefined
      };
      this.transactionService.update(this.transactionId!, payload).subscribe({
        next: () => { this.notify.success('Transacción actualizada.'); this.router.navigate(['/transactions']); },
        error: () => this.saving.set(false)
      });
    } else {
      const v = this.form.value;
      const payload = {
        type:      v.type as 'Purchase' | 'Sale',
        productId: v.productId!,
        quantity:  +v.quantity!,
        unitPrice: +v.unitPrice!,
        detail:    v.detail || undefined,
        date:      v.date ? new Date(v.date!).toISOString() : undefined
      };
      this.transactionService.create(payload).subscribe({
        next: () => { this.notify.success('Transacción registrada.'); this.router.navigate(['/transactions']); },
        error: () => this.saving.set(false)
      });
    }
  }
}

function maxDecimals(max: number) {
  return (c: AbstractControl): ValidationErrors | null => {
    const val = c.value;
    if (val === null || val === undefined || val === '') return null;
    const parts = val.toString().split('.');
    return parts.length > 1 && parts[1].length > max ? { maxDecimals: { max } } : null;
  };
}

function isInteger(c: AbstractControl): ValidationErrors | null {
  const val = c.value;
  if (val === null || val === undefined || val === '') return null;
  return Number.isInteger(Number(val)) ? null : { integer: true };
}