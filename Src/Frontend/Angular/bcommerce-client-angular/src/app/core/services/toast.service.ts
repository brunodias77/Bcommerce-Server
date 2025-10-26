import { Injectable, signal } from '@angular/core';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: string;
  type: ToastType;
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  private readonly _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();

  private show(type: ToastType, message: string): void {
    const toast: Toast = {
      id: Date.now().toString(),
      type,
      message,
    };

    this._toasts.update((toasts) => [...toasts, toast]);

    setTimeout(() => this.remove(toast.id), 3000);
  }
  
  remove(id: string): void {
    this._toasts.update((toasts) => toasts.filter((t) => t.id !== id));
  }
  
  success(message: string): void {
    this.show('success', message);
  }

  error(message: string): void {
    this.show('error', message);
  }

  warning(message: string): void {
    this.show('warning', message);
  }

  info(message: string): void {
    this.show('info', message);
  }
}
