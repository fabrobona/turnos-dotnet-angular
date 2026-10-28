import { HttpErrorResponse } from '@angular/common/http';

import { erroresPorCampo, mensajeDeError } from './errores';

const http = (status: number, error: unknown = null) => new HttpErrorResponse({ status, error });

describe('mensajeDeError', () => {
  it('usa el detalle que manda la API', () => {
    expect(mensajeDeError(http(409, { title: 'Conflicto', detail: 'Ya existe un turno en ese horario.' }))).toBe(
      'Ya existe un turno en ese horario.',
    );
  });

  it('cae en el título si no hay detalle', () => {
    expect(mensajeDeError(http(400, { title: 'Los datos enviados no son válidos.' }))).toBe(
      'Los datos enviados no son válidos.',
    );
  });

  it('avisa cuando no hay conexión', () => {
    expect(mensajeDeError(http(0))).toContain('conectar');
  });

  it('avisa cuando se superó el límite de solicitudes', () => {
    expect(mensajeDeError(http(429))).toContain('Demasiadas');
  });

  it('devuelve un mensaje genérico para cualquier otra cosa', () => {
    expect(mensajeDeError(new Error('boom'))).toBe('Ocurrió un error inesperado.');
    expect(mensajeDeError(http(500))).toBe('Ocurrió un error inesperado.');
  });
});

describe('erroresPorCampo', () => {
  it('devuelve los errores de validación de un 400', () => {
    const errores = { clienteNombre: ['Obligatorio'] };

    expect(erroresPorCampo(http(400, { errors: errores }))).toEqual(errores);
  });

  it('devuelve null si no es un 400 o no trae errores por campo', () => {
    expect(erroresPorCampo(http(409, { errors: { x: ['y'] } }))).toBeNull();
    expect(erroresPorCampo(http(400, { title: 'Mal' }))).toBeNull();
    expect(erroresPorCampo('algo')).toBeNull();
  });
});
