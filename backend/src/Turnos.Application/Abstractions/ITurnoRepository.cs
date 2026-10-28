using Turnos.Domain.Entities;

namespace Turnos.Application.Abstractions;

public interface ITurnoRepository
{
    Task<Turno?> ObtenerPorIdAsync(Guid id, CancellationToken ct);

    Task<IReadOnlyList<Turno>> ListarAsync(
        DateTime? desdeUtc, DateTime? hastaUtc, EstadoTurno? estado, CancellationToken ct);

    /// <summary>
    /// Indica si hay un turno no cancelado que se pisa con el rango [inicio, fin).
    /// </summary>
    Task<bool> ExisteSolapeAsync(DateTime inicioUtc, DateTime finUtc, Guid? excluirId, CancellationToken ct);

    Task AgregarAsync(Turno turno, CancellationToken ct);

    Task GuardarCambiosAsync(CancellationToken ct);
}
