import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/home',
    pathMatch: 'full'
  },
  {
    path: 'home',
    loadComponent: () => import('./features/home/pages/home/home').then(m => m.Home)
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/pages/login/login').then(m => m.Login)
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/pages/register/register').then(m => m.Register)
  },
  {
    path: 'auth/login',
    redirectTo: '/login',
    pathMatch: 'full'
  },
  {
    path: 'auth/register',
    redirectTo: '/register',
    pathMatch: 'full'
  }

];
