// ─── Auth ──────────────────────────────────────────────────────────────
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName?: string;
  lastName?: string;
  phone?: string;
  role?: string;
}

export interface AuthResponse {
  accessToken: string;
}

export interface DecodedToken {
  sub?: string;
  email?: string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string;
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'?: string;
  nameid?: string;
  unique_name?: string;
  exp?: number;
  [key: string]: unknown;
}

export interface CurrentUser {
  id: number;
  email: string;
  role: string;
}

// ─── Common ────────────────────────────────────────────────────────────
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
}

export interface PagedRequest {
  page: number;
  size: number;
  sortBy?: string;
  sortDir?: string;
}

export interface OrderPagedRequest {
  page: number;
  size: number;
  sortBy?: string;
  desc?: boolean;
  status?: string;
  from?: string;
  to?: string;
  userId?: number;
}

export interface UserPagedRequest {
  email?: string;
  role?: string;
  name?: string;
  sortBy?: string;
  desc?: boolean;
  page: number;
  size: number;
}

// ─── Category ──────────────────────────────────────────────────────────
export interface CategoryReadDto {
  id: number;
  name: string;
  description?: string;
  parentCategoryId?: number;
  createdUtc: string;
  updatedUtc?: string;
}

export interface CategoryCreateDto {
  name: string;
  description?: string;
  parentCategoryId?: number;
}

export interface CategoryUpdateDto extends CategoryCreateDto {
  id: number;
}

// ─── Product ───────────────────────────────────────────────────────────
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
}

// ─── Cart ──────────────────────────────────────────────────────────────
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

// ─── Orders ────────────────────────────────────────────────────────────
export interface PlaceOrderRequest {
  addressId: number;
  notes?: string;
  paymentType: string;
}

export interface PlaceOrderResponse {
  id: number;
  orderNumber: string;
  total: number;
  status: string;
  paymentStatus: string;
  placedAtUtc: string;
}

export interface OrderDetailDto {
  id: number;
  productId: number;
  productName: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

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
  items: OrderDetailDto[];
}

export interface OrderSummaryDto {
  id: number;
  orderNumber: string;
  status: string;
  placedAtUtc: string;
  total: number;
  itemsCount: number;
}

export interface CancelOrderResponse {
  id: number;
  status: string;
  message: string;
}

export interface UpdateOrderStatusRequest {
  status: string;
}

// ─── Address ───────────────────────────────────────────────────────────
export interface AddressReadDto {
  id: number;
  label?: string;
  fullName: string;
  phone?: string;
  line1: string;
  line2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  userId: number;
  createdUtc: string;
  updatedUtc?: string;
}

export interface AddressCreateDto {
  label?: string;
  fullName: string;
  phone?: string;
  line1: string;
  line2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
}

export interface AddressUpdateDto extends AddressCreateDto {}

// ─── Inventory ─────────────────────────────────────────────────────────
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

// ─── User ──────────────────────────────────────────────────────────────
export interface UserProfileReadDto {
  id: number;
  email: string;
  role: string;
  firstName: string;
  lastName: string;
  phone?: string;
  dateOfBirth?: string;
  createdUtc: string;
  updatedUtc?: string;
}

export interface UpdateUserProfileDto {
  firstName?: string;
  lastName?: string;
  phone?: string;
  dateOfBirth?: string;
}

export interface ChangePasswordDto {
  currentPassword: string;
  newPassword: string;
}

export interface UserListItemDto {
  id: number;
  email: string;
  role: string;
  fullName: string;
  phone?: string;
  createdUtc: string;
}

// ─── Review ────────────────────────────────────────────────────────────
export interface ReviewReadDto {
  id: number;
  productId: number;
  userId: number;
  rating: number;
  comment?: string;
  userName?: string;
  createdUtc: string;
}

export interface ReviewCreateDto {
  productId: number;
  rating: number;
  comment?: string;
}

export interface ReviewUpdateDto {
  rating: number;
  comment?: string;
}
