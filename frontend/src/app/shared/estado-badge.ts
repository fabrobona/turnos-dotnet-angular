import { Component, input } from '@angular/core';

import { EstadoTurno } from '../core/models';

@Component({
  selector: 'app-estado-badge',
  template: `<span class="insignia" [class]="'insignia estado-' + estado().toLowerCase()">{{ estado() }}</span>`,
  styles: `
    .insignia {
      display: inline-block;
      padding: 2px 10px;
      border-radius: 999px;
      font-size: 0.78rem;
      font-weight: 600;
      border: 1px solid transparent;
    }
    .estado-pendiente {
      background: var(--pendiente-fondo);
      color: var(--pendiente-texto);
    }
    .estado-confirmado {
      background: var(--confirmado-fondo);
      color: var(--confirmado-texto);
    }
    .estado-completado {
      background: var(--ok-fondo);
      color: var(--ok-texto);
    }
    .estado-cancelado {
      background: var(--fondo-suave);
      color: var(--texto-suave);
      text-decoration: line-through;
    }
  `,
})
export class EstadoBadge {
  readonly estado = input.required<EstadoTurno>();
}
