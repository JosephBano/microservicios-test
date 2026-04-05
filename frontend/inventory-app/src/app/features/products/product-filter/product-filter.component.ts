import { ChangeDetectionStrategy, Component, inject, output } from '@angular/core';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ProductFilter } from '../../../core/models/product.model';

@Component({
  selector: 'app-product-filter',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule],
  templateUrl: './product-filter.component.html'
})
export class ProductFilterComponent {
  readonly filterChange = output<ProductFilter>();

  readonly form = inject(FormBuilder).group({
    name:     [''],
    category: [''],
    minPrice: [null as number | null],
    maxPrice: [null as number | null],
    minStock: [null as number | null]
  });

  apply(): void {
    const v = this.form.value;
    const f: ProductFilter = {};
    if (v.name)             f.name     = v.name;
    if (v.category)         f.category = v.category;
    if (v.minPrice != null) f.minPrice = +v.minPrice;
    if (v.maxPrice != null) f.maxPrice = +v.maxPrice;
    if (v.minStock != null) f.minStock = +v.minStock;
    this.filterChange.emit(f);
  }

  clear(): void {
    this.form.reset();
    this.filterChange.emit({});
  }
}
