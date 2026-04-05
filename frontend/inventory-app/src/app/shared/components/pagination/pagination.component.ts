import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-pagination',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule],
  templateUrl: './pagination.component.html',
  styleUrl: './pagination.component.scss'
})
export class PaginationComponent {
  readonly currentPage = input.required<number>();
  readonly totalPages  = input.required<number>();
  readonly totalCount  = input.required<number>();
  readonly pageChange  = output<number>();

  readonly isFirst = computed(() => this.currentPage() <= 1);
  readonly isLast  = computed(() => this.currentPage() >= this.totalPages());

  go(page: number): void {
    if (page >= 1 && page <= this.totalPages()) {
      this.pageChange.emit(page);
    }
  }
}
