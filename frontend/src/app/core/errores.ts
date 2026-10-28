import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

/** Mensaje legible para mostrar a la persona, a partir de la respuesta (RFC 7807) de la API. */
export function mensajeDeError(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    if (err.status === 0) return 'No se pudo conectar con el servidor.';
    if (err.status === 429) return 'Demasiadas solicitudes. Probá de nuevo en un minuto.';

    const problema = err.error as ProblemDetails | null;
    if (problema?.detail) return problema.detail;
    if (problema?.title) return problema.title;
  }
  return 'Ocurrió un error inesperado.';
}

/** Errores de validación del servidor agrupados por campo (HTTP 400), o null si no aplica. */
export function erroresPorCampo(err: unknown): Record<string, string[]> | null {
  if (err instanceof HttpErrorResponse && err.status === 400) {
    return (err.error as ProblemDetails | null)?.errors ?? null;
  }
  return null;
}
