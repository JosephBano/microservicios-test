import { ChangeDetectionStrategy, Component, inject, input, OnInit, output } from '@angular/core';
import { ReactiveFormsModule, FormBuilder } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { TransactionFilter } from '../../../core/models/transaction.model';

@Component({
  selector: 'app-transaction-filter',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatDatepickerModule, MatNativeDateModule
  ],
  templateUrl: './transaction-filter.component.html'
})
export class TransactionFilterComponent implements OnInit {
  readonly filterChange     = output<TransactionFilter>();
  readonly initialProductId = input<string | null>(null);

  readonly form = inject(FormBuilder).group({
    type:     [''],
    dateFrom: [null as Date | null],
    dateTo:   [null as Date | null]
  });

  ngOnInit(): void {
    if (this.initialProductId()) this.apply();
  }

  apply(): void {
    const v = this.form.value;
    const f: TransactionFilter = {};
    if (this.initialProductId()) f.productId = this.initialProductId()!;
    if (v.type)     f.type    = v.type as 'Purchase' | 'Sale';
    if (v.dateFrom) f.dateFrom = new Date(v.dateFrom!).toISOString();
    if (v.dateTo)   f.dateTo   = new Date(v.dateTo!).toISOString();
    this.filterChange.emit(f);
  }

  clear(): void {
    this.form.reset({ type: '' });
    const f: TransactionFilter = {};
    if (this.initialProductId()) f.productId = this.initialProductId()!;
    this.filterChange.emit(f);
  }
}
