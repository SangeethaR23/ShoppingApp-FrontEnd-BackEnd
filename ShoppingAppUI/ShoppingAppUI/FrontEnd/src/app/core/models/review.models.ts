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
  userId: number;
  productId: number;
  rating: number;
  comment?: string;
}

export interface ReviewUpdateDto {
  rating: number;
  comment?: string;
}
