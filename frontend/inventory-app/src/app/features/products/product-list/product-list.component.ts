import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ProductService } from '../../../core/services/product.service';
import { NotificationService } from '../../../core/services/notification.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { ProductFilterComponent } from '../product-filter/product-filter.component';
import { Product, ProductFilter } from '../../../core/models/product.model';
import { PagedResponse } from '../../../core/models/paged-response.model';
import { pageEnter, listStagger } from '../../../core/animations/app.animations';

@Component({
  selector: 'app-product-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CurrencyPipe, MatButtonModule, MatIconModule, MatTooltipModule,
    PaginationComponent, ProductFilterComponent
  ],
  animations: [pageEnter, listStagger],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss'
})
export class ProductListComponent implements OnInit {
  private readonly productService = inject(ProductService);
  private readonly notify          = inject(NotificationService);
  private readonly dialog          = inject(MatDialog);
  readonly router                  = inject(Router);

  readonly products    = signal<Product[]>([]);
  readonly loading     = signal(false);
  readonly currentPage = signal(1);
  readonly totalPages  = signal(1);
  readonly totalCount  = signal(0);

  readonly okCount   = computed(() => this.products().filter(p => p.stock >= 5).length);
  readonly lowCount  = computed(() => this.products().filter(p => p.stock > 0 && p.stock < 5).length);
  readonly zeroCount = computed(() => this.products().filter(p => p.stock === 0).length);

  readonly skeletons = Array(8);
  private activeFilter: ProductFilter = {};

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading.set(true);
    this.productService.getAll({ ...this.activeFilter, page: this.currentPage() }).subscribe({
      next: (res: PagedResponse<Product>) => {
        this.products.set(res.data);
        this.totalPages.set(res.totalPages);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onFilterChange(filter: ProductFilter): void {
    this.activeFilter = filter;
    this.currentPage.set(1);
    this.load();
  }

  onPageChange(page: number): void {
    this.currentPage.set(page);
    this.load();
  }

  stockBadgeClass(stock: number): string {
    if (stock === 0) return 'badge badge-zero';
    if (stock < 5)  return 'badge badge-low';
    return 'badge badge-ok';
  }

  confirmDelete(product: Product): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Eliminar Producto',
        message: `¿Está seguro que desea eliminar "${product.name}"? Esta acción no se puede deshacer.`,
        confirmText: 'Eliminar'
      }
    }).afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.productService.delete(product.id).subscribe({
          next: () => {
            this.notify.success(`"${product.name}" eliminado correctamente.`);
            this.load();
          }
        });
      }
    });
  }

  onImgError(event: Event): void {
    (event.target as HTMLImageElement).style.display = 'none';
  }
}
