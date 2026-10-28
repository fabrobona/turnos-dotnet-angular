import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { NuevoTurno } from './models';
import { TurnosService } from './turnos.service';

describe('TurnosService', () => {
  let service: TurnosService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(TurnosService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('lista sin enviar parámetros cuando no hay filtros', () => {
    service.listar().subscribe();

    const req = http.expectOne('/api/turnos');
    expect(req.request.method).toBe('GET');
    expect(req.request.params.keys().length).toBe(0);
    req.flush([]);
  });

  it('filtra por estado y por día enviando el rango del día en UTC', () => {
    service.listar({ estado: 'Pendiente', dia: '2026-11-03' }).subscribe();

    const req = http.expectOne((r) => r.url === '/api/turnos');
    expect(req.request.params.get('estado')).toBe('Pendiente');
    expect(req.request.params.get('desde')).toBe(new Date('2026-11-03T00:00:00').toISOString());
    expect(req.request.params.get('hasta')).toBe(new Date('2026-11-04T00:00:00').toISOString());
    req.flush([]);
  });

  it('ignora los filtros vacíos', () => {
    service.listar({ estado: '', dia: '' }).subscribe();

    const req = http.expectOne('/api/turnos');
    expect(req.request.params.keys().length).toBe(0);
    req.flush([]);
  });

  it('crea un turno con POST', () => {
    const datos: NuevoTurno = {
      clienteNombre: 'Ana',
      clienteTelefono: null,
      servicio: 'Corte',
      inicio: '2026-11-03T14:00:00.000Z',
      duracionMinutos: 30,
      notas: null,
    };

    service.crear(datos).subscribe();

    const req = http.expectOne('/api/turnos');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(datos);
    req.flush({});
  });

  it('reprograma con PUT sobre /horario', () => {
    service.reprogramar('abc', { inicio: '2026-11-03T14:00:00.000Z', duracionMinutos: 45 }).subscribe();

    const req = http.expectOne('/api/turnos/abc/horario');
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual({ inicio: '2026-11-03T14:00:00.000Z', duracionMinutos: 45 });
    req.flush({});
  });

  it('cambia de estado con POST a la acción correspondiente', () => {
    for (const accion of ['confirmar', 'completar', 'cancelar'] as const) {
      service.cambiarEstado('abc', accion).subscribe();
      const req = http.expectOne(`/api/turnos/abc/${accion}`);
      expect(req.request.method).toBe('POST');
      req.flush({});
    }
  });

  it('envía el mensaje a interpretar', () => {
    service.interpretarMensaje('hola').subscribe();

    const req = http.expectOne('/api/turnos/interpretar-mensaje');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ mensaje: 'hola' });
    req.flush({});
  });

  it('obtiene un turno por id', () => {
    service.obtener('abc').subscribe();

    const req = http.expectOne('/api/turnos/abc');
    expect(req.request.method).toBe('GET');
    req.flush({});
  });
});
