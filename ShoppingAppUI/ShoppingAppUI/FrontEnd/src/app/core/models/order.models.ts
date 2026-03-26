export interface OrderDetailDto {
  id: number;
  productId: number;
  productName: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

/** Required by spec — same shape as OrderDetailDto */
export type OrderItemDto = OrderDetailDto;

export interface OrderReadDto {
  id: number;
  orderNumber: string;
  status: string;
  paymentStatus: string;
  placedAtUtc: string;
  shipToName: string;
  shipToPhone?: string;
  shipToLine1: string;
  shipToLine2?: string;
  shipToCity: string;
  shipToState: string;
  shipToPostalCode: string;
  shipToCountry: string;
  subTotal: number;
  shippingFee: number;
  discount: number;
  total: number;
  items: OrderItemDto[];
}

export interface OrderSummaryDto {
  id: number;
  orderNumber: string;
  status: string;
  placedAtUtc: string;
  total: number;
  itemsCount: number;
}

export interface PlaceOrderRequestDto {
  userId: number;
  addressId: number;
  notes?: string;
  paymentType: string;
  walletUseAmount: number;
  promoCode?: string;
}

export interface PlaceOrderResponseDto {
  id: number;
  orderNumber: string;
  total: number;
  status: string;
  paymentStatus: string;
  placedAtUtc: string;
}

export interface CancelOrderResponseDto {
  id: number;
  status: string;
  message: string;
}

export interface OrderPagedRequest {
  status?: string;
  from?: string;
  to?: string;
  userId?: number;
  page: number;
  size: number;
  sortBy?: string;
  desc?: boolean;
}

export interface UpdateOrderStatusRequest {
  status: string;
}

export const ORDER_STATUSES = [
  'Pending', 'Confirmed', 'Shipped', 'Delivered',
  'Cancelled', 'ReturnRequested', 'ReturnApproved',
  'ReturnRejected', 'Returned'
];

export const PAYMENT_TYPES = [
  'CashOnDelivery', 'UPI', 'Card', 'NetBanking', 'Wallet'
];
