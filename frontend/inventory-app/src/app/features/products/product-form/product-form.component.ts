import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ProductService } from '../../../core/services/product.service';
import { NotificationService } from '../../../core/services/notification.service';
import { pageEnter, scaleIn } from '../../../core/animations/app.animations';

@Component({
  selector: 'app-product-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule
  ],
  animations: [pageEnter, scaleIn],
  templateUrl: './product-form.component.html',
  styleUrl: './product-form.component.scss'
})
export class ProductFormComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly notify         = inject(NotificationService);
  private readonly route          = inject(ActivatedRoute);
  readonly router                 = inject(Router);

  readonly isEdit      = signal(false);
  readonly saving      = signal(false);
  readonly loadingData = signal(false);
  private productId: string | null = null;

  readonly form = inject(FormBuilder).group({
    name:        ['', [Validators.required, Validators.maxLength(150)]],
    description: ['', Validators.maxLength(500)],
    category:    ['', [Validators.required, Validators.maxLength(100)]],
    price:       [0, [Validators.required, Validators.min(0), Validators.max(999999.99), maxDecimals(2)]],
    stock:       [0, [Validators.required, Validators.min(0), Validators.max(999999), isInteger]],
    imageUrl:    ['', [Validators.maxLength(500), Validators.pattern(/^https?:\/\/.+/)]]
  });

  ngOnInit(): void {
    this.productId = this.route.snapshot.paramMap.get('id');
    if (this.productId) {
      this.isEdit.set(true);
      this.loadProduct(this.productId);
    }
  }

  private loadProduct(id: string): void {
    this.loadingData.set(true);
    this.productService.getById(id).subscribe({
      next: p => {
        this.form.patchValue({
          name: p.name, description: p.description ?? '',
          category: p.category, price: p.price,
          stock: p.stock, imageUrl: p.imageUrl ?? ''
        });
        this.loadingData.set(false);
      },
      error: () => { this.loadingData.set(false); this.router.navigate(['/products']); }
    });
  }

  submit(): void {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);

    const v = this.form.value;
    const payload = {
      name:        v.name!,
      description: v.description || undefined,
      category:    v.category!,
      price:       +v.price!,
      stock:       +v.stock!,
      imageUrl:    v.imageUrl || undefined
    };

    const action = this.isEdit()
      ? this.productService.update(this.productId!, payload)
      : this.productService.create(payload);

    action.subscribe({
      next: () => {
        this.notify.success(this.isEdit() ? 'Producto actualizado.' : 'Producto creado.');
        this.router.navigate(['/products']);
      },
      error: () => this.saving.set(false)
    });
  }

  onImgError(event: Event): void {
    (event.target as HTMLImageElement).style.display = 'none';
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