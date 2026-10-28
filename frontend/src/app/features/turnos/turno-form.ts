import { Component, OnInit, computed, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { erroresPorCampo, mensajeDeError } from '../../core/errores';
import { aFechaHoraLocal, aIsoUtc } from '../../core/fechas';
import { Turno, TurnoSugerido } from '../../core/models';
import { Notificaciones } from '../../core/notificaciones.service';
import { TurnosService } from '../../core/turnos.service';

const CAMPOS_SOLO_ALTA = ['clienteNombre', 'clienteTelefono', 'servicio', 'notas'] as const;

type Campo = 'clienteNombre' | 'clienteTelefono' | 'servicio' | 'fecha' | 'hora' | 'duracionMinutos' | 'notas';

@Component({
  selector: 'app-turno-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './turno-form.html',
  styleUrl: './turno-form.css',
})
export class TurnoForm implements OnInit {
  private readonly fb = inject(FormBuilder).nonNullable;
  private readonly turnosService = inject(TurnosService);
  private readonly avisos = inject(Notificaciones);
  private readonly router = inject(Router);

  /** Llega desde la ruta /turnos/:id/editar. Sin id, el formulario crea un turno nuevo. */
  readonly id = input<string>();

  protected readonly editando = computed(() => !!this.id());
  protected readonly guardando = signal(false);
  protected readonly cargando = signal(false);
  protected readonly errorGeneral = signal<string | null>(null);

  protected readonly mensaje = signal('');
  protected readonly interpretando = signal(false);
  protected readonly sugerencia = signal<TurnoSugerido | null>(null);
  protected readonly faltantes = computed(() => {
    const s = this.sugerencia();
    if (!s) return [];
    const faltan: string[] = [];
    if (!s.clienteNombre) faltan.push('nombre');
    if (!s.servicio) faltan.push('servicio');
    if (!s.inicio) faltan.push('fecha y hora');
    return faltan;
  });

  protected readonly form = this.fb.group({
    clienteNombre: ['', [Validators.required, Validators.maxLength(100)]],
    clienteTelefono: ['', [Validators.maxLength(30), Validators.pattern(/^[0-9+()\-\s]*$/)]],
    servicio: ['', [Validators.required, Validators.maxLength(100)]],
    fecha: ['', Validators.required],
    hora: ['', Validators.required],
    duracionMinutos: [30, [Validators.required, Validators.min(5), Validators.max(480)]],
    notas: ['', Validators.maxLength(500)],
  });

  ngOnInit(): void {
    const id = this.id();
    if (!id) return;

    CAMPOS_SOLO_ALTA.forEach((campo) => this.form.controls[campo].disable());
    this.cargando.set(true);
    this.turnosService.obtener(id).subscribe({
      next: (turno) => {
        this.cargarTurno(turno);
        this.cargando.set(false);
      },
      error: (err) => {
        this.errorGeneral.set(mensajeDeError(err));
        this.cargando.set(false);
      },
    });
  }

  protected alEscribirMensaje(evento: Event): void {
    this.mensaje.set((evento.target as HTMLTextAreaElement).value);
  }

  protected interpretar(): void {
    const texto = this.mensaje().trim();
    if (!texto) return;

    this.interpretando.set(true);
    this.turnosService.interpretarMensaje(texto).subscribe({
      next: (sugerencia) => {
        this.sugerencia.set(sugerencia);
        this.aplicar(sugerencia);
        this.interpretando.set(false);
      },
      error: (err) => {
        this.avisos.error(mensajeDeError(err));
        this.interpretando.set(false);
      },
    });
  }

  protected guardar(): void {
    this.errorGeneral.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const inicio = aIsoUtc(v.fecha, v.hora);
    this.guardando.set(true);

    const peticion = this.editando()
      ? this.turnosService.reprogramar(this.id()!, { inicio, duracionMinutos: v.duracionMinutos })
      : this.turnosService.crear({
          clienteNombre: v.clienteNombre,
          clienteTelefono: v.clienteTelefono || null,
          servicio: v.servicio,
          inicio,
          duracionMinutos: v.duracionMinutos,
          notas: v.notas || null,
        });

    peticion.subscribe({
      next: () => {
        this.avisos.ok(this.editando() ? 'Turno reprogramado.' : 'Turno creado.');
        void this.router.navigate(['/turnos']);
      },
      error: (err) => {
        this.guardando.set(false);
        this.mostrarErrores(err);
      },
    });
  }

  /** Mensaje de error de un campo, solo cuando la persona ya lo tocó. */
  protected errorDe(campo: Campo): string | null {
    const control = this.form.controls[campo];
    if (!control.touched || !control.errors) return null;

    const errores = control.errors;
    if (errores['server']) return errores['server'] as string;
    if (errores['required']) return 'Este campo es obligatorio.';
    if (errores['maxlength']) return `Máximo ${errores['maxlength'].requiredLength} caracteres.`;
    if (errores['pattern']) return 'Solo números, espacios y + - ( ).';
    if (errores['min'] || errores['max']) return 'La duración debe estar entre 5 y 480 minutos.';
    return 'Valor no válido.';
  }

  private cargarTurno(turno: Turno): void {
    const { fecha, hora } = aFechaHoraLocal(turno.inicio);
    this.form.patchValue({
      clienteNombre: turno.clienteNombre,
      clienteTelefono: turno.clienteTelefono ?? '',
      servicio: turno.servicio,
      fecha,
      hora,
      duracionMinutos: turno.duracionMinutos,
      notas: turno.notas ?? '',
    });
  }

  private aplicar(s: TurnoSugerido): void {
    this.form.patchValue({
      ...(s.clienteNombre ? { clienteNombre: s.clienteNombre } : {}),
      ...(s.servicio ? { servicio: s.servicio } : {}),
      ...(s.notas ? { notas: s.notas } : {}),
      duracionMinutos: s.duracionMinutos,
      ...(s.inicio ? aFechaHoraLocal(s.inicio) : {}),
    });
  }

  private mostrarErrores(err: unknown): void {
    const porCampo = erroresPorCampo(err);
    if (porCampo) {
      for (const [campo, mensajes] of Object.entries(porCampo)) {
        const control = this.form.get(campo);
        control?.setErrors({ server: mensajes[0] });
        control?.markAsTouched();
      }
    }
    this.errorGeneral.set(porCampo ? 'Revisá los campos marcados.' : mensajeDeError(err));
  }
}
