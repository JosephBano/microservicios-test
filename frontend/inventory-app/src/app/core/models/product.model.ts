export interface Product {
  id: string;
  name: string;
  description?: string;
  category: string;
  imageUrl?: string;
  price: number;
  stock: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProductRequest {
  name: string;
  description?: string;
  category: string;
  imageUrl?: string;
  price: number;
  stock: number;
}

export interface UpdateProductRequest extends CreateProductRequest {}

export interface ProductFilter {
  name?: string;
  category?: string;
  minPrice?: number;
  maxPrice?: number;
  minStock?: number;
  page?: number;
  pageSize?: number;
}
