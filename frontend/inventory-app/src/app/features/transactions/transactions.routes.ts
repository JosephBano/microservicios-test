import { Routes } from '@angular/router';

export const transactionRoutes: Routes = [
  {
    path: '',
    loadComponent: () => import('./transaction-list/transaction-list.component').then(m => m.TransactionListComponent)
  },
  {
    path: 'new',
    loadComponent: () => import('./transaction-form/transaction-form.component').then(m => m.TransactionFormComponent)
  },
  {
    path: ':id/edit',
    loadComponent: () => import('./transaction-form/transaction-form.component').then(m => m.TransactionFormComponent)
  }
];
