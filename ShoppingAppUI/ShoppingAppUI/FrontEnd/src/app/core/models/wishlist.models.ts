export interface WishlistReadDto {
  productId: number;
  productName: string;
  sku: string;
  price: number;
  isActive: boolean;
}

export interface WishlistToggleDto {
  productId: number;
}
