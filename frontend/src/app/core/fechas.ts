const dos = (n: number): string => String(n).padStart(2, '0');

/** Convierte una fecha y una hora escritas por la persona (hora local) a un instante ISO en UTC. */
export function aIsoUtc(fecha: string, hora: string): string {
  return new Date(`${fecha}T${hora}`).toISOString();
}

/** Descompone un instante ISO en la fecha y la hora locales que muestran los campos del formulario. */
export function aFechaHoraLocal(iso: string): { fecha: string; hora: string } {
  const d = new Date(iso);
  return {
    fecha: `${d.getFullYear()}-${dos(d.getMonth() + 1)}-${dos(d.getDate())}`,
    hora: `${dos(d.getHours())}:${dos(d.getMinutes())}`,
  };
}

/** Rango [desde, hasta) en UTC que cubre un día completo en hora local. */
export function rangoDelDia(dia: string): { desde: string; hasta: string } {
  const desde = new Date(`${dia}T00:00:00`);
  const hasta = new Date(desde);
  hasta.setDate(hasta.getDate() + 1);
  return { desde: desde.toISOString(), hasta: hasta.toISOString() };
}
