import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { aIsoUtc } from '../../core/fechas';
import { Turno, TurnoSugerido } from '../../core/models';
import { TurnosService } from '../../core/turnos.service';
import { TurnoForm } from './turno-form';

const turnoGuardado: Turno = {
  id: 'abc',
  clienteNombre: 'Ana Pérez',
  clienteTelefono: '600 111 222',
  servicio: 'Corte',
  inicio: aIsoUtc('2026-11-03', '15:30'),
  fin: aIsoUtc('2026-11-03', '16:00'),
  duracionMinutos: 30,
  estado: 'Pendiente',
  notas: null,
};

describe('TurnoForm', () => {
  let fixture: ComponentFixture<TurnoForm>;
  let servicio: jasmine.SpyObj<TurnosService>;
  let router: Router;

  const html = () => fixture.nativeElement as HTMLElement;
  const campo = (nombre: string) => html().querySelector(`[formControlName="${nombre}"]`) as HTMLInputElement;
  const boton = (texto: string) =>
    Array.from(html().querySelectorAll('button')).find((b) => b.textContent?.includes(texto)) as HTMLButtonElement;

  function escribir(elemento: HTMLInputElement | HTMLTextAreaElement, valor: string) {
    elemento.value = valor;
    elemento.dispatchEvent(new Event('input'));
  }

  function completarFormulario() {
    escribir(campo('clienteNombre'), 'Ana Pérez');
    escribir(campo('servicio'), 'Corte');
    escribir(campo('fecha'), '2026-11-03');
    escribir(campo('hora'), '15:30');
  }

  async function crear(id?: string) {
    fixture = TestBed.createComponent(TurnoForm);
    if (id) fixture.componentRef.setInput('id', id);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  beforeEach(() => {
    servicio = jasmine.createSpyObj<TurnosService>('TurnosService', [
      'crear',
      'reprogramar',
      'obtener',
      'interpretarMensaje',
    ]);
    TestBed.configureTestingModule({
      imports: [TurnoForm],
      providers: [{ provide: TurnosService, useValue: servicio }, provideRouter([])],
    });
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
  });

  it('no guarda un formulario vacío y marca los campos obligatorios', async () => {
    await crear();

    boton('Crear turno').click();
    fixture.detectChanges();

    expect(servicio.crear).not.toHaveBeenCalled();
    expect(Array.from(html().querySelectorAll('.error')).map((e) => e.textContent?.trim())).toEqual(
      jasmine.arrayContaining(['Este campo es obligatorio.']),
    );
    expect(html().querySelectorAll('.error').length).toBe(4);
  });

  it('valida el teléfono y la duración', async () => {
    await crear();

    escribir(campo('clienteTelefono'), 'abc');
    escribir(campo('duracionMinutos'), '3');
    campo('clienteTelefono').dispatchEvent(new Event('blur'));
    campo('duracionMinutos').dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    const errores = Array.from(html().querySelectorAll('.error')).map((e) => e.textContent);
    expect(errores).toContain('Solo números, espacios y + - ( ).');
    expect(errores).toContain('La duración debe estar entre 5 y 480 minutos.');
  });

  it('crea el turno convirtiendo la fecha y hora locales a UTC y vuelve a la agenda', async () => {
    await crear();
    servicio.crear.and.returnValue(of(turnoGuardado));

    completarFormulario();
    boton('Crear turno').click();

    expect(servicio.crear).toHaveBeenCalledOnceWith({
      clienteNombre: 'Ana Pérez',
      clienteTelefono: null,
      servicio: 'Corte',
      inicio: new Date('2026-11-03T15:30').toISOString(),
      duracionMinutos: 30,
      notas: null,
    });
    expect(router.navigate).toHaveBeenCalledWith(['/turnos']);
  });

  it('muestra el conflicto de horario y no navega', async () => {
    await crear();
    servicio.crear.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 409, error: { detail: 'Ya existe un turno en ese horario.' } })),
    );

    completarFormulario();
    boton('Crear turno').click();
    fixture.detectChanges();

    expect(html().querySelector('.alerta-error')?.textContent).toContain('Ya existe un turno en ese horario.');
    expect(router.navigate).not.toHaveBeenCalled();
    expect(boton('Crear turno').disabled).toBeFalse();
  });

  it('asigna los errores de validación del servidor a cada campo', async () => {
    await crear();
    servicio.crear.and.returnValue(
      throwError(
        () => new HttpErrorResponse({ status: 400, error: { errors: { servicio: ['Servicio no ofrecido.'] } } }),
      ),
    );

    completarFormulario();
    boton('Crear turno').click();
    fixture.detectChanges();

    expect(html().textContent).toContain('Servicio no ofrecido.');
    expect(html().querySelector('.alerta-error')?.textContent).toContain('Revisá los campos marcados.');
  });

  describe('pegar un mensaje', () => {
    const sugerencia: TurnoSugerido = {
      clienteNombre: 'Ana García',
      servicio: 'Corte',
      inicio: aIsoUtc('2026-11-03', '16:00'),
      duracionMinutos: 45,
      notas: null,
      origen: 'reglas',
    };

    it('completa los campos que entendió e indica cómo lo interpretó', async () => {
      await crear();
      servicio.interpretarMensaje.and.returnValue(of(sugerencia));

      escribir(html().querySelector('#mensaje') as HTMLTextAreaElement, 'soy Ana García, un corte mañana a las 16');
      fixture.detectChanges();
      boton('Interpretar mensaje').click();
      fixture.detectChanges();

      expect(servicio.interpretarMensaje).toHaveBeenCalledWith('soy Ana García, un corte mañana a las 16');
      expect(campo('clienteNombre').value).toBe('Ana García');
      expect(campo('servicio').value).toBe('Corte');
      expect(campo('fecha').value).toBe('2026-11-03');
      expect(campo('hora').value).toBe('16:00');
      expect(campo('duracionMinutos').value).toBe('45');
      expect(html().querySelector('.resultado')?.textContent).toContain('reglas básicas');
    });

    it('avisa qué datos no pudo deducir y no pisa lo que ya estaba escrito', async () => {
      await crear();
      escribir(campo('servicio'), 'Peinado');
      servicio.interpretarMensaje.and.returnValue(
        of({ ...sugerencia, servicio: null, inicio: null, origen: 'gemini' as const }),
      );

      escribir(html().querySelector('#mensaje') as HTMLTextAreaElement, 'hola, soy Ana García');
      fixture.detectChanges();
      boton('Interpretar mensaje').click();
      fixture.detectChanges();

      expect(campo('servicio').value).toBe('Peinado');
      const resultado = html().querySelector('.resultado')?.textContent;
      expect(resultado).toContain('IA (Gemini)');
      expect(resultado).toContain('servicio, fecha y hora');
    });

    it('no ofrece interpretar mensajes al reprogramar', async () => {
      servicio.obtener.and.returnValue(of(turnoGuardado));

      await crear('abc');

      expect(html().querySelector('#mensaje')).toBeNull();
    });
  });

  describe('reprogramar', () => {
    it('carga el turno, bloquea los datos del cliente y envía solo el nuevo horario', async () => {
      servicio.obtener.and.returnValue(of(turnoGuardado));
      servicio.reprogramar.and.returnValue(of(turnoGuardado));

      await crear('abc');

      expect(servicio.obtener).toHaveBeenCalledWith('abc');
      expect(html().querySelector('h1')?.textContent).toContain('Reprogramar');
      expect(campo('clienteNombre').disabled).toBeTrue();
      expect(campo('clienteNombre').value).toBe('Ana Pérez');
      expect(campo('hora').value).toBe('15:30');

      escribir(campo('hora'), '17:00');
      boton('Guardar cambios').click();

      expect(servicio.reprogramar).toHaveBeenCalledOnceWith('abc', {
        inicio: new Date('2026-11-03T17:00').toISOString(),
        duracionMinutos: 30,
      });
      expect(router.navigate).toHaveBeenCalledWith(['/turnos']);
    });

    it('muestra el error si no se puede cargar el turno', async () => {
      servicio.obtener.and.returnValue(
        throwError(() => new HttpErrorResponse({ status: 404, error: { detail: 'No existe el turno abc.' } })),
      );

      await crear('abc');

      expect(html().querySelector('.alerta-error')?.textContent).toContain('No existe el turno abc.');
    });
  });
});
