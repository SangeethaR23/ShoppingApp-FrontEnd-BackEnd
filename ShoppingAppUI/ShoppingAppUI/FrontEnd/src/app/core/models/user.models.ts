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

export interface ChangePasswordRequestDto {
  userId: number;
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

export interface UserPagedRequest {
  email?: string;
  role?: string;
  name?: string;
  sortBy?: string;
  desc?: boolean;
  page: number;
  size: number;
}
