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
  id: number;
  sub: string;
  email: string;
  role: string;
  exp: number;
}
