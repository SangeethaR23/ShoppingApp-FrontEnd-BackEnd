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
  id?: number;
}
