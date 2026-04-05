import { Injectable, signal } from '@angular/core';

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error' | 'info';
}

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private _toasts = signal<Toast[]>([]);
  readonly toasts = this._toasts.asReadonly();
  private nextId = 0;

  success(message: string, duration = 4000): void {
    this.addToast(message, 'success', duration);
  }

  error(message: string, duration = 6000): void {
    this.addToast(message, 'error', duration);
  }

  info(message: string, duration = 4000): void {
    this.addToast(message, 'info', duration);
  }

  dismiss(id: number): void {
    this._toasts.update(toasts => toasts.filter(t => t.id !== id));
  }

  private addToast(message: string, type: Toast['type'], duration: number): void {
    const id = this.nextId++;
    this._toasts.update(toasts => [...toasts, { id, message, type }]);
    setTimeout(() => this.dismiss(id), duration);
  }
}
