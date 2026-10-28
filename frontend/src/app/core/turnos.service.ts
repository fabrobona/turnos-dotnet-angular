import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { rangoDelDia } from './fechas';
import {
  AccionTurno,
  FiltroTurnos,
  NuevoTurno,
  Reprogramacion,
  Turno,
  TurnoSugerido,
} from './models';

@Injectable({ providedIn: 'root' })
export class TurnosService {
  private readonly http = inject(HttpClient);
  private readonly base = '/api/turnos';

  listar(filtro: FiltroTurnos = {}): Observable<Turno[]> {
    let params = new HttpParams();
    if (filtro.estado) params = params.set('estado', filtro.estado);
    if (filtro.dia) {
      const { desde, hasta } = rangoDelDia(filtro.dia);
      params = params.set('desde', desde).set('hasta', hasta);
    }
    return this.http.get<Turno[]>(this.base, { params });
  }

  obtener(id: string): Observable<Turno> {
    return this.http.get<Turno>(`${this.base}/${id}`);
  }

  crear(datos: NuevoTurno): Observable<Turno> {
    return this.http.post<Turno>(this.base, datos);
  }

  reprogramar(id: string, datos: Reprogramacion): Observable<Turno> {
    return this.http.put<Turno>(`${this.base}/${id}/horario`, datos);
  }

  cambiarEstado(id: string, accion: AccionTurno): Observable<Turno> {
    return this.http.post<Turno>(`${this.base}/${id}/${accion}`, {});
  }

  interpretarMensaje(mensaje: string): Observable<TurnoSugerido> {
    return this.http.post<TurnoSugerido>(`${this.base}/interpretar-mensaje`, { mensaje });
  }
}
