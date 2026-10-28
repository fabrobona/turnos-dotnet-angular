import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { Turno } from '../../core/models';
import { TurnosService } from '../../core/turnos.service';
import { TurnosList } from './turnos-list';

const enHoras = (horas: number) => new Date(Date.now() + horas * 3_600_000).toISOString();

function turno(parche: Partial<Turno>): Turno {
  return {
    id: 'id-1',
    clienteNombre: 'Ana Pérez',
    clienteTelefono: '600 111 222',
    servicio: 'Corte',
    inicio: enHoras(24),
    fin: enHoras(24.5),
    duracionMinutos: 30,
    estado: 'Pendiente',
    notas: null,
    ...parche,
  };
}

describe('TurnosList', () => {
  let fixture: ComponentFixture<TurnosList>;
  let servicio: jasmine.SpyObj<TurnosService>;

  const pendiente = turno({ id: 'pendiente', clienteNombre: 'Pendiente Pérez' });
  const empezado = turno({
    id: 'empezado',
    clienteNombre: 'Empezado Gómez',
    estado: 'Confirmado',
    inicio: enHoras(-1),
    fin: enHoras(-0.5),
  });
  const futuroConfirmado = turno({ id: 'futuro', clienteNombre: 'Futuro Ruiz', estado: 'Confirmado' });
  const cancelado = turno({ id: 'cancelado', clienteNombre: 'Cancelado López', estado: 'Cancelado' });

  const html = () => fixture.nativeElement as HTMLElement;
  const filas = () => Array.from(html().querySelectorAll('tbody tr'));
  const botones = (fila: Element) => Array.from(fila.querySelectorAll('button, a.btn')) as HTMLElement[];
  const botonDe = (idFila: number, texto: string) =>
    botones(filas()[idFila]).find((b) => b.textContent?.trim() === texto) as HTMLButtonElement | undefined;

  async function crear(turnos: Turno[] = [pendiente, empezado, futuroConfirmado, cancelado]) {
    servicio.listar.and.returnValue(of(turnos));
    fixture = TestBed.createComponent(TurnosList);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
  }

  beforeEach(() => {
    servicio = jasmine.createSpyObj<TurnosService>('TurnosService', ['listar', 'cambiarEstado']);
    TestBed.configureTestingModule({
      imports: [TurnosList],
      providers: [{ provide: TurnosService, useValue: servicio }, provideRouter([])],
    });
  });

  it('muestra una fila por turno y el resumen por estado', async () => {
    await crear();

    expect(filas().length).toBe(4);
    const resumen = Array.from(html().querySelectorAll('.resumen li')).map((li) =>
      Array.from(li.children)
        .map((hijo) => hijo.textContent?.trim())
        .join(' '),
    );
    expect(resumen).toEqual(['Pendiente 1', 'Confirmado 2', 'Completado 0', 'Cancelado 1']);
  });

  it('muestra un mensaje cuando no hay turnos', async () => {
    await crear([]);

    expect(html().querySelector('.vacio')?.textContent).toContain('No hay turnos');
  });

  it('ofrece las acciones que corresponden a cada estado', async () => {
    await crear();

    expect(botones(filas()[0]).map((b) => b.textContent?.trim())).toEqual(['Confirmar', 'Reprogramar', 'Cancelar']);
    expect(botones(filas()[1]).map((b) => b.textContent?.trim())).toEqual(['Completar', 'Reprogramar', 'Cancelar']);
    expect(botones(filas()[3])).toEqual([]);
  });

  it('solo deja completar un turno confirmado que ya empezó', async () => {
    await crear();

    expect(botonDe(1, 'Completar')?.disabled).toBeFalse();
    expect(botonDe(2, 'Completar')?.disabled).toBeTrue();
  });

  it('confirmar llama al servicio y actualiza la fila sin recargar', async () => {
    await crear();
    servicio.cambiarEstado.and.returnValue(of({ ...pendiente, estado: 'Confirmado' }));

    botonDe(0, 'Confirmar')!.click();
    fixture.detectChanges();

    expect(servicio.cambiarEstado).toHaveBeenCalledWith('pendiente', 'confirmar');
    expect(filas()[0].querySelector('app-estado-badge')?.textContent).toContain('Confirmado');
    expect(servicio.listar).toHaveBeenCalledTimes(1);
  });

  it('cancelar pide confirmación y no hace nada si la persona se arrepiente', async () => {
    await crear();
    spyOn(window, 'confirm').and.returnValue(false);

    botonDe(0, 'Cancelar')!.click();

    expect(window.confirm).toHaveBeenCalled();
    expect(servicio.cambiarEstado).not.toHaveBeenCalled();
  });

  it('cancelar con confirmación llama al servicio', async () => {
    await crear();
    spyOn(window, 'confirm').and.returnValue(true);
    servicio.cambiarEstado.and.returnValue(of({ ...pendiente, estado: 'Cancelado' }));

    botonDe(0, 'Cancelar')!.click();

    expect(servicio.cambiarEstado).toHaveBeenCalledWith('pendiente', 'cancelar');
  });

  it('al filtrar por estado vuelve a consultar con ese filtro', async () => {
    await crear();
    const select = html().querySelector('select') as HTMLSelectElement;

    select.value = 'Cancelado';
    select.dispatchEvent(new Event('change'));

    expect(servicio.listar).toHaveBeenCalledWith({ estado: 'Cancelado', dia: '' });
  });

  it('muestra el error del servidor y permite reintentar', async () => {
    servicio.listar.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 500, error: { detail: 'Falló la base' } })),
    );
    fixture = TestBed.createComponent(TurnosList);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(html().querySelector('.alerta-error')?.textContent).toContain('Falló la base');

    servicio.listar.and.returnValue(of([pendiente]));
    (html().querySelector('.alerta-error button') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(filas().length).toBe(1);
  });
});
