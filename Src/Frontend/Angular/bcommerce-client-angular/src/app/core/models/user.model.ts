/**
 * Interface do usuário baseada na API do backend
 */
export interface User {
  id: string;
  fullName: string;
  email: string;
  phone?: string;
  birthDate?: string;
  createdAt: string;
  updatedAt: string;
  lastLoginAt?: string;
}

/**
 * Interface para dados de registro de usuário
 */
export interface RegisterUserRequest {
  fullName: string;
  email: string;
  password: string;
  confirmPassword: string;
  phone?: string;
  birthDate?: string;
}

/**
 * Interface para dados de login
 */
export interface LoginRequest {
  email: string;
  password: string;
}

/**
 * Interface para tokens de autenticação
 */
export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  expiresIn: number;
}

/**
 * Interface para resposta de autenticação
 */
export interface AuthResponse {
  user: User;
  tokens: AuthTokens;
}