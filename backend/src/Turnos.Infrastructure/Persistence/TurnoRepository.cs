using Microsoft.EntityFrameworkCore;
using Turnos.Application.Abstractions;
using Turnos.Domain.Entities;

namespace Turnos.Infrastructure.Persistence;

public class TurnoRepository(TurnosDbContext db) : ITurnoRepository
{
    public Task<Turno?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        db.Turnos.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<Turno>> ListarAsync(
        DateTime? desdeUtc, DateTime? hastaUtc, EstadoTurno? estado, CancellationToken ct)
    {
        var consulta = db.Turnos.AsNoTracking().AsQueryable();

        if (desdeUtc is not null) consulta = consulta.Where(t => t.Inicio >= desdeUtc);
        if (hastaUtc is not null) consulta = consulta.Where(t => t.Inicio < hastaUtc);
        if (estado is not null) consulta = consulta.Where(t => t.Estado == estado);

        return await consulta.OrderBy(t => t.Inicio).ToListAsync(ct);
    }

    public Task<bool> ExisteSolapeAsync(DateTime inicioUtc, DateTime finUtc, Guid? excluirId, CancellationToken ct) =>
        db.Turnos.AnyAsync(t =>
            t.Estado != EstadoTurno.Cancelado
            && t.Inicio < finUtc
            && inicioUtc < t.Fin
            && (excluirId == null || t.Id != excluirId.Value), ct);

    public async Task AgregarAsync(Turno turno, CancellationToken ct) =>
        await db.Turnos.AddAsync(turno, ct);

    public Task GuardarCambiosAsync(CancellationToken ct) => db.SaveChangesAsync(ct);
}
