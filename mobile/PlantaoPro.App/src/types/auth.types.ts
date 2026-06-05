export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T | null;
  errors?: string[] | null;
  statusCode: number;
  timestamp?: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalItems?: number;
  total?: number;
  totalPages?: number;
}

export interface LoginRequest { email: string; senha: string; }
export interface LoginResponse { token: string; refreshToken?: string | null; expiresAtUtc: string; roles: string[]; }
export interface UserProfile { id?: string; nome: string; email: string; perfil?: string; telefone?: string; }
