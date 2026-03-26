export interface CartItemReadDto {
  id: number;
  productId: number;
  productName: string;
  sku: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  averageRating: number;
  reviewsCount: number;
}

export interface CartReadDto {
  id: number;
  userId: number;
  items: CartItemReadDto[];
  subTotal: number;
}

export interface CartAddItemDto {
  productId: number;
  quantity: number;
}

export interface CartUpdateItemDto {
  productId: number;
  quantity: number;
}
