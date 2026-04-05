import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ProductService } from '../../../core/services/product.service';
import { TransactionService } from '../../../core/services/transaction.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { Product } from '../../../core/models/product.model';
import { Transaction } from '../../../core/models/transaction.model';
import { pageEnter } from '../../../core/animations/app.animations';

@Component({
  selector: 'app-product-history',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CurrencyPipe, DatePipe, ReactiveFormsModule, MatTableModule, MatButtonModule,
    MatIconModule, MatFormFieldModule, MatSelectModule, MatInputModule,
    MatDatepickerModule, MatNativeDateModule, MatProgressSpinnerModule,
    MatTooltipModule, PaginationComponent
  ],
  animations: [pageEnter],
  templateUrl: './product-history.component.html',
  styleUrl: './product-history.component.scss'
})
export class ProductHistoryComponent implements OnInit {
  private readonly productService     = inject(ProductService);
  private readonly transactionService = inject(TransactionService);
  private readonly route              = inject(ActivatedRoute);
  readonly router                     = inject(Router);

  readonly cols         = ['date', 'type', 'quantity', 'unitPrice', 'totalPrice', 'detail'];
  readonly product      = signal<Product | null>(null);
  readonly transactions = signal<Transaction[]>([]);
  readonly loading      = signal(false);
  readonly currentPage  = signal(1);
  readonly totalPages   = signal(1);
  readonly totalCount   = signal(0);

  private productId!: string;

  readonly filterForm = inject(FormBuilder).group({
    type:     [''],
    dateFrom: [null as Date | null],
    dateTo:   [null as Date | null]
  });

  ngOnInit(): void {
    this.productId = this.route.snapshot.paramMap.get('id')!;
    this.loadProduct();
    this.loadTransactions();
  }

  private loadProduct(): void {
    this.productService.getById(this.productId).subscribe({
      next: p => this.product.set(p),
      error: () => this.router.navigate(['/products'])
    });
  }

  loadTransactions(): void {
    this.loading.set(true);
    const v = this.filterForm.value;
    this.transactionService.getAll({
      productId: this.productId,
      type:      v.type as 'Purchase' | 'Sale' | undefined || undefined,
      dateFrom:  v.dateFrom ? new Date(v.dateFrom!).toISOString() : undefined,
      dateTo:    v.dateTo   ? new Date(v.dateTo!).toISOString()   : undefined,
      page:      this.currentPage(),
      pageSize:  10
    }).subscribe({
      next: res => {
        this.transactions.set(res.data);
        this.totalPages.set(res.totalPages);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  applyFilter(): void { this.currentPage.set(1); this.loadTransactions(); }

  clearFilter(): void {
    this.filterForm.reset({ type: '' });
    this.currentPage.set(1);
    this.loadTransactions();
  }

  onPageChange(page: number): void { this.currentPage.set(page); this.loadTransactions(); }
}
