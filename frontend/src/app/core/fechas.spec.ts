import { aFechaHoraLocal, aIsoUtc, rangoDelDia } from './fechas';

describe('fechas', () => {
  it('aIsoUtc devuelve un instante en UTC (con Z)', () => {
    expect(aIsoUtc('2026-11-03', '15:30')).toMatch(/Z$/);
  });

  it('aIsoUtc y aFechaHoraLocal son inversas entre sí', () => {
    const iso = aIsoUtc('2026-11-03', '15:30');

    expect(aFechaHoraLocal(iso)).toEqual({ fecha: '2026-11-03', hora: '15:30' });
  });

  it('rellena con ceros las horas y fechas de un dígito', () => {
    const iso = aIsoUtc('2026-01-05', '08:05');

    expect(aFechaHoraLocal(iso)).toEqual({ fecha: '2026-01-05', hora: '08:05' });
  });

  it('rangoDelDia cubre un día completo y empieza a medianoche local', () => {
    const { desde, hasta } = rangoDelDia('2026-11-03');

    expect(new Date(desde).getTime()).toBe(new Date('2026-11-03T00:00:00').getTime());
    expect(new Date(hasta).getTime()).toBe(new Date('2026-11-04T00:00:00').getTime());
  });
});
