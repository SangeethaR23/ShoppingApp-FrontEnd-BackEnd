export interface InventoryReadDto {
  id: number;
  productId: number;
  productName: string;
  sku: string;
  quantity: number;
  reorderLevel: number;
  createdUtc: string;
  updatedUtc?: string;
}

export interface InventoryAdjustRequestDto {
  delta: number;
  reason?: string;
}

export interface InventorySetRequestDto {
  quantity: number;
}

export interface InventoryReorderLevelRequestDto {
  reorderLevel: number;
}
