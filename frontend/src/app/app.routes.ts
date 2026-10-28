import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'turnos' },
  {
    path: 'turnos',
    title: 'Agenda · Gestor de turnos',
    loadComponent: () => import('./features/turnos/turnos-list').then((m) => m.TurnosList),
  },
  {
    path: 'turnos/nuevo',
    title: 'Nuevo turno · Gestor de turnos',
    loadComponent: () => import('./features/turnos/turno-form').then((m) => m.TurnoForm),
  },
  {
    path: 'turnos/:id/editar',
    title: 'Reprogramar turno · Gestor de turnos',
    loadComponent: () => import('./features/turnos/turno-form').then((m) => m.TurnoForm),
  },
  { path: '**', redirectTo: 'turnos' },
];
