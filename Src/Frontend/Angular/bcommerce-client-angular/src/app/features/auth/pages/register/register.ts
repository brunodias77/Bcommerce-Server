import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../../core/services/auth.service';
import { RegisterUserRequest } from '../../../../core/models/user.model';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.html',
  styleUrl: './register.css',
})
export class Register {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  // Signals para gerenciamento de estado
  isLoading = signal(false);
  errorMessage = signal<string | null>(null);
  showPassword = signal(false);
  showConfirmPassword = signal(false);

  // Formulário reativo
  registerForm: FormGroup;

  // Computed para validações
  isFormValid = computed(() => {
    if (!this.registerForm) {
      console.log('🔍 Form not initialized yet');
      return false;
    }
    
    const formValid = this.registerForm.valid;
    const passwordsMatch = this.passwordsMatch();
    const result = formValid && passwordsMatch;
    
    console.log('🔍 Debug isFormValid:', {
      formValid,
      passwordsMatch,
      result,
      formErrors: this.registerForm.errors,
      formStatus: this.registerForm.status,
      allFieldsStatus: this.getAllFieldsStatus()
    });
    
    return result;
  });
  
  passwordsMatch = computed(() => {
    const password = this.registerForm?.get('password')?.value;
    const confirmPassword = this.registerForm?.get('confirmPassword')?.value;
    const match = password === confirmPassword;
    console.log('🔍 Debug passwordsMatch:', { password, confirmPassword, match });
    return match;
  });

  constructor() {
    this.registerForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [
        Validators.required,
        Validators.minLength(8),
        this.strongPasswordValidator
      ]],
      confirmPassword: ['', [Validators.required]],
      fullName: ['', [Validators.required, Validators.minLength(2)]],
      phone: [''], // Campo opcional sem validação
      birthDate: ['']
    });
  }

  // Validador personalizado para senha forte
  private strongPasswordValidator(control: any) {
    const value = control.value;
    if (!value) return null;

    const hasUpperCase = /[A-Z]/.test(value);
    const hasLowerCase = /[a-z]/.test(value);
    const hasNumeric = /[0-9]/.test(value);
    
    const valid = hasUpperCase && hasLowerCase && hasNumeric;
    return valid ? null : { strongPassword: true };
  }

  // Validador personalizado para telefone
  private phoneValidator(control: any) {
    const value = control.value;
    console.log('🔍 Debug phoneValidator:', { value, isEmpty: !value });
    
    // Campo opcional - se estiver vazio, é válido
    if (!value || value.trim() === '') {
      console.log('📞 Phone field is empty - valid');
      return null;
    }
    
    // Regex mais flexível para telefone
    const phoneRegex = /^\(\d{2}\)\s?\d{4,5}-?\d{4}$/;
    const isValid = phoneRegex.test(value);
    console.log('📞 Phone validation:', { value, isValid, regex: phoneRegex.toString() });
    
    return isValid ? null : { invalidPhone: true };
  }

  // Alternar visibilidade da senha
  togglePasswordVisibility() {
    this.showPassword.update(show => !show);
  }

  toggleConfirmPasswordVisibility() {
    this.showConfirmPassword.update(show => !show);
  }

  // Obter mensagem de erro para um campo
  getFieldError(fieldName: string): string | null {
    const field = this.registerForm.get(fieldName);
    if (!field || !field.errors || !field.touched) return null;

    const errors = field.errors;
    
    if (errors['required']) return `${this.getFieldLabel(fieldName)} é obrigatório`;
    if (errors['email']) return 'Email inválido';
    if (errors['minlength']) {
      const requiredLength = errors['minlength'].requiredLength;
      return `${this.getFieldLabel(fieldName)} deve ter pelo menos ${requiredLength} caracteres`;
    }
    if (errors['strongPassword']) return 'Senha deve conter maiúscula, minúscula e número';
    if (errors['invalidPhone']) return 'Formato: (11) 99999-9999';

    return null;
  }

  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      email: 'Email',
      password: 'Senha',
      confirmPassword: 'Confirmação de senha',
      fullName: 'Nome completo',
      phone: 'Telefone',
      birthDate: 'Data de nascimento'
    };
    return labels[fieldName] || fieldName;
  }

  // Verificar se campo tem erro
  hasFieldError(fieldName: string): boolean {
    const field = this.registerForm.get(fieldName);
    return !!(field && field.errors && field.touched);
  }

  // Submeter formulário
  async onSubmit() {
    if (!this.registerForm.valid) {
      this.markAllFieldsAsTouched();
      return;
    }

    if (!this.passwordsMatch()) {
      this.errorMessage.set('As senhas não coincidem');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);

    try {
      const formValue = this.registerForm.value;
      const registerData: RegisterUserRequest = {
        email: formValue.email,
        password: formValue.password,
        confirmPassword: formValue.confirmPassword,
        fullName: formValue.fullName,
        phone: formValue.phone || undefined,
        birthDate: formValue.birthDate || undefined
      };

      const response = await this.authService.register(registerData);

      if (response.success) {
        // Registro bem-sucedido, redirecionar para dashboard
        await this.router.navigate(['/dashboard']);
      } else {
        this.errorMessage.set(response.message || 'Erro ao criar conta');
      }
    } catch (error: any) {
      this.errorMessage.set(error?.message || 'Erro inesperado ao criar conta');
    } finally {
      this.isLoading.set(false);
    }
  }

  private markAllFieldsAsTouched() {
    Object.keys(this.registerForm.controls).forEach(key => {
      this.registerForm.get(key)?.markAsTouched();
    });
  }

  // Método auxiliar para debug
  private getAllFieldsStatus() {
    const fields = ['email', 'password', 'confirmPassword', 'fullName', 'phone', 'birthDate'];
    const status: any = {};
    fields.forEach(field => {
      const control = this.registerForm.get(field);
      status[field] = {
        value: control?.value,
        valid: control?.valid,
        errors: control?.errors,
        touched: control?.touched
      };
    });
    return status;
  }
}
