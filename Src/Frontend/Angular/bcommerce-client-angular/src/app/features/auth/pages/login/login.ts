import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { LoginRequest } from '../../../../core/models/user.model';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private router = inject(Router);

  // Signals para estado do componente
  showPassword = signal(false);
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);

  // Formulário reativo
  loginForm: FormGroup;

  // Computed properties
  isFormValid = computed(() => {
    const form = this.loginForm;
    return form?.valid && !this.isLoading();
  });

  constructor() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  // Alternar visibilidade da senha
  togglePasswordVisibility() {
    this.showPassword.update(show => !show);
  }

  // Verificar se campo tem erro
  hasFieldError(fieldName: string): boolean {
    const field = this.loginForm.get(fieldName);
    return !!(field && field.errors && field.touched);
  }

  // Obter mensagem de erro do campo
  getFieldError(fieldName: string): string {
    const field = this.loginForm.get(fieldName);
    if (!field || !field.errors || !field.touched) return '';

    const errors = field.errors;
    
    if (errors['required']) return `${this.getFieldLabel(fieldName)} é obrigatório`;
    if (errors['email']) return 'Email deve ter um formato válido';
    if (errors['minlength']) return `${this.getFieldLabel(fieldName)} deve ter pelo menos ${errors['minlength'].requiredLength} caracteres`;
    
    return 'Campo inválido';
  }

  // Obter label do campo
  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      email: 'Email',
      password: 'Senha'
    };
    return labels[fieldName] || fieldName;
  }

  // Realizar login
  async onSubmit() {
    if (!this.loginForm.valid) {
      this.markAllFieldsAsTouched();
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    try {
      const loginData: LoginRequest = {
        email: this.loginForm.get('email')?.value,
        password: this.loginForm.get('password')?.value
      };

      const response = await this.authService.login(loginData);

      if (response.success) {
        // Mostrar toast de sucesso
        this.toastService.success('Login realizado com sucesso! Bem-vindo!');
        
        // Redirecionar para perfil do usuário
        await this.router.navigate(['/profile']);
      } else {
        this.errorMessage.set(response.message || 'Erro ao fazer login');
        // Mostrar toast de erro
        this.toastService.error(response.message || 'Erro ao fazer login');
      }
    } catch (error: any) {
      console.error('Erro no login:', error);
      this.errorMessage.set('Erro interno. Tente novamente.');
      // Mostrar toast de erro para exceções
      this.toastService.error('Erro interno. Tente novamente.');
    } finally {
      this.isLoading.set(false);
    }
  }

  // Marcar todos os campos como tocados para mostrar erros
  private markAllFieldsAsTouched() {
    Object.keys(this.loginForm.controls).forEach(key => {
      this.loginForm.get(key)?.markAsTouched();
    });
  }

  // Limpar mensagem de erro
  clearError() {
    this.errorMessage.set(null);
  }
}
