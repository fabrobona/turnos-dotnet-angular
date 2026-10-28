import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { mensajeDeError } from '../../core/errores';
import { AccionTurno, ESTADOS, EstadoTurno, Turno } from '../../core/models';
import { Notificaciones } from '../../core/notificaciones.service';
import { TurnosService } from '../../core/turnos.service';
import { EstadoBadge } from '../../shared/estado-badge';

const MENSAJE_EXITO: Record<AccionTurno, string> = {
  confirmar: 'Turno confirmado.',
  completar: 'Turno completado.',
  cancelar: 'Turno cancelado. El horario quedó libre.',
};

@Component({
  selector: 'app-turnos-list',
  imports: [DatePipe, RouterLink, EstadoBadge],
  templateUrl: './turnos-list.html',
  styleUrl: './turnos-list.css',
})
export class TurnosList implements OnInit {
  private readonly turnosService = inject(TurnosService);
  private readonly avisos = inject(Notificaciones);

  protected readonly estados = ESTADOS;
  protected readonly estado = signal<EstadoTurno | ''>('');
  protected readonly dia = signal('');
  protected readonly turnos = signal<Turno[]>([]);
  protected readonly cargando = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly resumen = computed(() =>
    ESTADOS.map((estado) => ({
      estado,
      cantidad: this.turnos().filter((t) => t.estado === estado).length,
    })),
  );

  ngOnInit(): void {
    this.cargar();
  }

  protected cargar(): void {
    this.cargando.set(true);
    this.error.set(null);

    this.turnosService.listar({ estado: this.estado(), dia: this.dia() }).subscribe({
      next: (turnos) => {
        this.turnos.set(turnos);
        this.cargando.set(false);
      },
      error: (err) => {
        this.error.set(mensajeDeError(err));
        this.cargando.set(false);
      },
    });
  }

  protected alCambiarEstado(evento: Event): void {
    this.estado.set((evento.target as HTMLSelectElement).value as EstadoTurno | '');
    this.cargar();
  }

  protected alCambiarDia(evento: Event): void {
    this.dia.set((evento.target as HTMLInputElement).value);
    this.cargar();
  }

  protected limpiarFiltros(): void {
    this.estado.set('');
    this.dia.set('');
    this.cargar();
  }

  protected hayFiltros(): boolean {
    return this.estado() !== '' || this.dia() !== '';
  }

  protected esActivo(turno: Turno): boolean {
    return turno.estado === 'Pendiente' || turno.estado === 'Confirmado';
  }

  protected yaEmpezo(turno: Turno): boolean {
    return new Date(turno.inicio).getTime() <= Date.now();
  }

  protected cambiar(turno: Turno, accion: AccionTurno): void {
    this.turnosService.cambiarEstado(turno.id, accion).subscribe({
      next: (actualizado) => {
        this.reemplazar(actualizado);
        this.avisos.ok(MENSAJE_EXITO[accion]);
      },
      error: (err) => this.avisos.error(mensajeDeError(err)),
    });
  }

  protected cancelar(turno: Turno): void {
    if (!confirm(`¿Cancelar el turno de ${turno.clienteNombre}?`)) return;
    this.cambiar(turno, 'cancelar');
  }

  private reemplazar(actualizado: Turno): void {
    const filtro = this.estado();
    this.turnos.update((lista) =>
      lista
        .map((t) => (t.id === actualizado.id ? actualizado : t))
        .filter((t) => !filtro || t.estado === filtro),
    );
  }
}
