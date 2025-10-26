import { Injectable, inject, signal, computed, effect } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { 
  User, 
  LoginRequest, 
  RegisterUserRequest, 
  AuthTokens, 
  AuthResponse, 
  ApiResponse 
} from '../models';

/**
 * Serviço de autenticação usando Signals e sintaxe moderna do Angular 20
 */
@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  
  private readonly API_BASE_URL = 'http://localhost:5050/api/auth';
  private readonly TOKEN_KEY = 'auth_tokens';
  private readonly USER_KEY = 'current_user';

  // Signals para gerenciamento de estado
  private readonly _currentUser = signal<User | null>(null);
  private readonly _tokens = signal<AuthTokens | null>(null);
  private readonly _isLoading = signal<boolean>(false);

  // Computed signals
  readonly currentUser = this._currentUser.asReadonly();
  readonly tokens = this._tokens.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly isAuthenticated = computed(() => {
    const tokens = this._tokens();
    return tokens !== null && this.isTokenValid(tokens.accessToken);
  });

  constructor() {
    // Effect para sincronizar com localStorage
    effect(() => {
      const tokens = this._tokens();
      const user = this._currentUser();
      
      if (tokens && user) {
        localStorage.setItem(this.TOKEN_KEY, JSON.stringify(tokens));
        localStorage.setItem(this.USER_KEY, JSON.stringify(user));
      } else {
        localStorage.removeItem(this.TOKEN_KEY);
        localStorage.removeItem(this.USER_KEY);
      }
    });

    // Carregar dados do localStorage na inicialização
    this.loadFromStorage();
  }

  /**
   * Realiza login do usuário
   */
  async login(credentials: LoginRequest): Promise<ApiResponse<any>> {
    this._isLoading.set(true);
    
    try {
      const response = await firstValueFrom(
        this.http.post<ApiResponse<any>>(
          `${this.API_BASE_URL}/login`,
          credentials
        )
      );

      if (response?.success && response.data) {
        // O backend retorna os dados diretamente em response.data
        const loginData = response.data;
        
        // Criar objeto User a partir dos dados retornados
        const user: User = {
          id: loginData.userId,
          fullName: loginData.fullName,
          email: loginData.email,
          phone: '', // Não retornado no login
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString()
        };

        // Criar objeto AuthTokens a partir dos dados retornados
        const tokens: AuthTokens = {
          accessToken: loginData.accessToken,
          refreshToken: loginData.refreshToken,
          expiresIn: loginData.expiresIn
        };

        this._currentUser.set(user);
        this._tokens.set(tokens);
      }

      return response;
    } catch (error) {
      return this.handleError(error);
    } finally {
      this._isLoading.set(false);
    }
  }

  /**
   * Realiza registro de novo usuário
   */
  async register(userData: RegisterUserRequest): Promise<ApiResponse<AuthResponse>> {
    this._isLoading.set(true);
    
    try {
      const response = await firstValueFrom(
        this.http.post<ApiResponse<AuthResponse>>(
          `${this.API_BASE_URL}/register`,
          userData
        )
      );

      if (response?.success && response.data) {
        this._currentUser.set(response.data.user);
        this._tokens.set(response.data.tokens);
      }

      return response;
    } catch (error) {
      return this.handleError(error);
    } finally {
      this._isLoading.set(false);
    }
  }

  /**
   * Realiza logout do usuário
   */
  async logout(): Promise<void> {
    this._isLoading.set(true);
    
    try {
      const tokens = this._tokens();
      if (tokens) {
        // Opcional: chamar endpoint de logout no backend
        await firstValueFrom(
          this.http.post(`${this.API_BASE_URL}/logout`, {
            refreshToken: tokens.refreshToken
          })
        );
      }
    } catch (error) {
      console.warn('Erro ao fazer logout no servidor:', error);
    } finally {
      this.clearAuthData();
      this._isLoading.set(false);
      this.router.navigate(['/login']);
    }
  }

  /**
   * Atualiza o access token usando o refresh token
   */
  async refreshToken(): Promise<boolean> {
    const tokens = this._tokens();
    if (!tokens?.refreshToken) {
      this.clearAuthData();
      return false;
    }

    this._isLoading.set(true);
    
    try {
      const response = await firstValueFrom(
        this.http.post<ApiResponse<AuthTokens>>(
          `${this.API_BASE_URL}/refresh`,
          { refreshToken: tokens.refreshToken }
        )
      );

      if (response?.success && response.data) {
        this._tokens.set(response.data);
        return true;
      } else {
        this.clearAuthData();
        return false;
      }
    } catch (error) {
      console.error('Erro ao renovar token:', error);
      this.clearAuthData();
      return false;
    } finally {
      this._isLoading.set(false);
    }
  }

  /**
   * Obtém o access token atual
   */
  getAccessToken(): string | null {
    return this._tokens()?.accessToken || null;
  }

  /**
   * Verifica se o token ainda é válido
   */
  private isTokenValid(token: string): boolean {
    if (!token) return false;
    
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const currentTime = Math.floor(Date.now() / 1000);
      return payload.exp > currentTime;
    } catch {
      return false;
    }
  }

  /**
   * Carrega dados do localStorage
   */
  private loadFromStorage(): void {
    try {
      const storedTokens = localStorage.getItem(this.TOKEN_KEY);
      const storedUser = localStorage.getItem(this.USER_KEY);

      if (storedTokens && storedUser) {
        const tokens: AuthTokens = JSON.parse(storedTokens);
        const user: User = JSON.parse(storedUser);

        if (this.isTokenValid(tokens.accessToken)) {
          this._tokens.set(tokens);
          this._currentUser.set(user);
        } else {
          this.clearAuthData();
        }
      }
    } catch (error) {
      console.error('Erro ao carregar dados do localStorage:', error);
      this.clearAuthData();
    }
  }

  /**
   * Limpa todos os dados de autenticação
   */
  private clearAuthData(): void {
    this._currentUser.set(null);
    this._tokens.set(null);
  }

  /**
   * Trata erros das requisições HTTP
   */
  private handleError(error: any): ApiResponse<any> {
    if (error instanceof HttpErrorResponse) {
      // Tentar extrair a mensagem de diferentes estruturas possíveis do backend
      let errorMessage = 'Erro na comunicação com o servidor';
      
      if (error.error) {
        // Caso 1: error.error.errors[0].message (estrutura do backend atual)
        if (error.error.errors && Array.isArray(error.error.errors) && error.error.errors.length > 0 && error.error.errors[0].message) {
          errorMessage = error.error.errors[0].message;
        }
        // Caso 2: error.error.message (estrutura padrão)
        else if (error.error.message) {
          errorMessage = error.error.message;
        }
        // Caso 3: error.error.Message (com M maiúsculo - padrão .NET)
        else if (error.error.Message) {
          errorMessage = error.error.Message;
        }
        // Caso 4: error.error é uma string direta
        else if (typeof error.error === 'string') {
          errorMessage = error.error;
        }
        // Caso 5: error.error.title (algumas APIs usam title)
        else if (error.error.title) {
          errorMessage = error.error.title;
        }
        // Caso 6: error.error.detail (algumas APIs usam detail)
        else if (error.error.detail) {
          errorMessage = error.error.detail;
        }
      }
      
      return {
        success: false,
        message: errorMessage,
        errors: error.error?.errors || [errorMessage]
      };
    }

    return {
      success: false,
      message: 'Erro inesperado',
      errors: [error?.message || 'Erro desconhecido']
    };
  }
}