import { Component, inject } from '@angular/core';

import { Notificaciones } from '../core/notificaciones.service';

@Component({
  selector: 'app-avisos',
  template: `
    <div class="avisos" aria-live="polite">
      @for (aviso of notificaciones.avisos(); track aviso.id) {
        <div class="aviso" [class.aviso-error]="aviso.tipo === 'error'" role="status">
          <span>{{ aviso.texto }}</span>
          <button type="button" aria-label="Cerrar aviso" (click)="notificaciones.cerrar(aviso.id)">×</button>
        </div>
      }
    </div>
  `,
  styles: `
    .avisos {
      position: fixed;
      top: 68px;
      right: 16px;
      display: grid;
      gap: 8px;
      z-index: 50;
      max-width: min(380px, calc(100vw - 32px));
    }
    .aviso {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 12px;
      padding: 12px 14px;
      border-radius: 10px;
      background: var(--ok-fondo);
      color: var(--ok-texto);
      border: 1px solid var(--ok-borde);
      box-shadow: 0 6px 20px rgb(0 0 0 / 12%);
      font-size: 0.92rem;
    }
    .aviso-error {
      background: var(--error-fondo);
      color: var(--error-texto);
      border-color: var(--error-borde);
    }
    button {
      background: none;
      border: 0;
      color: inherit;
      font-size: 1.2rem;
      line-height: 1;
      cursor: pointer;
      opacity: 0.7;
    }
  `,
})
export class Avisos {
  protected readonly notificaciones = inject(Notificaciones);
}
