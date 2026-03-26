export interface PromoReadDto {
  id: number;
  code: string;
  discountAmount: number;
  isActive: boolean;
  minOrderAmount?: number;
  startDateUtc: string;
  endDateUtc: string;
}

export interface PromoCreateDto {
  code: string;
  discountAmount: number;
  startDateUtc: string;
  endDateUtc: string;
  minOrderAmount?: number;
}

export interface ApplyPromoDto {
  promoCode: string;
  cartTotal: number;
}
