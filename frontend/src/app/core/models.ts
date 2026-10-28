export const ESTADOS = ['Pendiente', 'Confirmado', 'Completado', 'Cancelado'] as const;

export type EstadoTurno = (typeof ESTADOS)[number];

export interface Turno {
  id: string;
  clienteNombre: string;
  clienteTelefono: string | null;
  servicio: string;
  /** Fecha y hora de inicio en UTC (ISO 8601). */
  inicio: string;
  fin: string;
  duracionMinutos: number;
  estado: EstadoTurno;
  notas: string | null;
}

export interface NuevoTurno {
  clienteNombre: string;
  clienteTelefono: string | null;
  servicio: string;
  inicio: string;
  duracionMinutos: number;
  notas: string | null;
}

export interface Reprogramacion {
  inicio: string;
  duracionMinutos: number;
}

/** Datos deducidos de un mensaje libre. Lo que no se entendió llega en null. */
export interface TurnoSugerido {
  clienteNombre: string | null;
  servicio: string | null;
  inicio: string | null;
  duracionMinutos: number;
  notas: string | null;
  origen: 'gemini' | 'reglas';
}

export interface FiltroTurnos {
  estado?: EstadoTurno | '';
  /** Día en formato yyyy-MM-dd, en hora local. */
  dia?: string;
}

export type AccionTurno = 'confirmar' | 'completar' | 'cancelar';
