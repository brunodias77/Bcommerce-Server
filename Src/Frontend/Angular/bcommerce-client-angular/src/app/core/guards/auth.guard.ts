import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

/**
 * Guard de autenticação funcional para Angular 20
 * Protege rotas que requerem autenticação
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Verifica se o usuário está autenticado usando o computed signal
  const isAuthenticated = authService.isAuthenticated();

  if (isAuthenticated) {
    // Usuário autenticado, permite acesso
    return true;
  } else {
    // Usuário não autenticado, redireciona para login
    // Preserva a URL de destino para redirecionamento após login
    router.navigate(['/login'], { 
      queryParams: { returnUrl: state.url } 
    });
    return false;
  }
};