export interface ProductImageReadDto {
  id: number;
  url: string;
}

export interface ProductReadDto {
  id: number;
  name: string;
  sku: string;
  price: number;
  categoryId: number;
  isActive: boolean;
  averageRating: number;
  reviewsCount: number;
  images: ProductImageReadDto[];
  description?: string;
}

export interface ProductCreateDto {
  name: string;
  sku: string;
  price: number;
  categoryId: number;
  description?: string;
  isActive: boolean;
}

export interface ProductUpdateDto extends ProductCreateDto {
  id: number;
}

export interface ProductQuery {
  categoryId?: number;
  includeChildren?: boolean;
  nameContains?: string;
  priceMin?: number;
  priceMax?: number;
  ratingMin?: number;
  sortBy?: string;
  sortDir?: string;
  page: number;
  size: number;
  inStockOnly?: boolean;
}

export interface ProductImageCreateDto {
  url: string;
}

export interface PagedRequestDto {
  page: number;
  size: number;
  sortBy?: string;
  sortDir?: string;
}
