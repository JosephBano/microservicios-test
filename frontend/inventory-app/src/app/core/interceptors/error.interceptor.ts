import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notify = inject(NotificationService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let message = 'Ha ocurrido un error inesperado.';

      if (error.status === 0) {
        message = 'No se puede conectar con el servidor. Verifique que los servicios estén ejecutándose.';
      } else if (error.error?.message) {
        message = error.error.message;
      } else if (error.error?.errors && Array.isArray(error.error.errors)) {
        message = error.error.errors.join(' | ');
      } else if (error.status === 404) {
        message = 'Recurso no encontrado.';
      } else if (error.status === 409) {
        message = error.error?.message ?? 'Conflicto: stock insuficiente.';
      } else if (error.status === 422) {
        message = error.error?.message ?? 'Datos inválidos.';
      } else if (error.status >= 500) {
        message = 'Error interno del servidor. Intente más tarde.';
      }

      notify.error(message);
      return throwError(() => error);
    })
  );
};
