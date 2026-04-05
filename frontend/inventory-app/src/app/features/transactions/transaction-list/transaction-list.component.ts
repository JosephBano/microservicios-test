import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { TransactionService } from '../../../core/services/transaction.service';
import { NotificationService } from '../../../core/services/notification.service';
import { PaginationComponent } from '../../../shared/components/pagination/pagination.component';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';
import { TransactionFilterComponent } from '../transaction-filter/transaction-filter.component';
import { Transaction, TransactionFilter } from '../../../core/models/transaction.model';
import { PagedResponse } from '../../../core/models/paged-response.model';
import { pageEnter } from '../../../core/animations/app.animations';

@Component({
  selector: 'app-transaction-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CurrencyPipe, DatePipe, MatTableModule, MatButtonModule, MatIconModule,
    MatTooltipModule, MatProgressSpinnerModule,
    PaginationComponent, TransactionFilterComponent
  ],
  animations: [pageEnter],
  templateUrl: './transaction-list.component.html',
  styleUrl: './transaction-list.component.scss'
})
export class TransactionListComponent implements OnInit {
  private readonly transactionService = inject(TransactionService);
  private readonly notify             = inject(NotificationService);
  private readonly dialog             = inject(MatDialog);
  private readonly route              = inject(ActivatedRoute);
  readonly router                     = inject(Router);

  readonly cols            = ['date', 'type', 'product', 'quantity', 'unitPrice', 'totalPrice', 'detail', 'actions'];
  readonly transactions    = signal<Transaction[]>([]);
  readonly loading         = signal(false);
  readonly currentPage     = signal(1);
  readonly totalPages      = signal(1);
  readonly totalCount      = signal(0);
  readonly productFilterId = signal<string | null>(null);

  readonly purchaseCount = computed(() => this.transactions().filter(t => t.type === 'Purchase').length);
  readonly saleCount     = computed(() => this.transactions().filter(t => t.type === 'Sale').length);
  readonly pageTotal     = computed(() => this.transactions().reduce((acc, t) => acc + t.totalPrice, 0));

  private activeFilter: TransactionFilter = {};

  ngOnInit(): void {
    const productId = this.route.snapshot.queryParamMap.get('productId');
    if (productId) {
      this.productFilterId.set(productId);
      this.activeFilter = { productId };
    }
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.transactionService.getAll({ ...this.activeFilter, page: this.currentPage() }).subscribe({
      next: (res: PagedResponse<Transaction>) => {
        this.transactions.set(res.data);
        this.totalPages.set(res.totalPages);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onFilterChange(filter: TransactionFilter): void {
    this.activeFilter = filter;
    this.currentPage.set(1);
    this.load();
  }

  onPageChange(page: number): void { this.currentPage.set(page); this.load(); }

  clearProductFilter(): void {
    this.productFilterId.set(null);
    this.activeFilter = {};
    this.router.navigate(['/transactions']);
    this.load();
  }

  confirmDelete(t: Transaction): void {
    this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Eliminar Transacción',
        message: 'Se eliminará la transacción y se revertirá el ajuste de stock correspondiente.',
        confirmText: 'Eliminar'
      }
    }).afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.transactionService.delete(t.id).subscribe({
          next: () => {
            this.notify.success('Transacción eliminada y stock revertido.');
            this.load();
          }
        });
      }
    });
  }
}
