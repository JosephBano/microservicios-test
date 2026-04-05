export type TransactionType = 'Purchase' | 'Sale';

export interface Transaction {
  id: string;
  date: string;
  type: TransactionType;
  productId: string;
  productName?: string;
  productStock?: number;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  detail?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateTransactionRequest {
  type: TransactionType;
  productId: string;
  quantity: number;
  unitPrice: number;
  detail?: string;
  date?: string;
}

export interface UpdateTransactionRequest {
  detail?: string;
  date?: string;
}

export interface TransactionFilter {
  productId?: string;
  type?: TransactionType | '';
  dateFrom?: string;
  dateTo?: string;
  page?: number;
  pageSize?: number;
}
