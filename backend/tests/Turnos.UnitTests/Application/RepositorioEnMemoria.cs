using Turnos.Application.Abstractions;
using Turnos.Domain.Entities;

namespace Turnos.UnitTests.Application;

/// <summary>Repositorio falso para probar los casos de uso sin base de datos.</summary>
public class RepositorioEnMemoria : ITurnoRepository
{
    private readonly List<Turno> _turnos = [];

    public int Guardados { get; private set; }

    public IReadOnlyList<Turno> Todos => _turnos;

    public Task<Turno?> ObtenerPorIdAsync(Guid id, CancellationToken ct) =>
        Task.FromResult(_turnos.FirstOrDefault(t => t.Id == id));

    public Task<IReadOnlyList<Turno>> ListarAsync(
        DateTime? desdeUtc, DateTime? hastaUtc, EstadoTurno? estado, CancellationToken ct)
    {
        IEnumerable<Turno> consulta = _turnos;
        if (desdeUtc is not null) consulta = consulta.Where(t => t.Inicio >= desdeUtc);
        if (hastaUtc is not null) consulta = consulta.Where(t => t.Inicio < hastaUtc);
        if (estado is not null) consulta = consulta.Where(t => t.Estado == estado);

        return Task.FromResult<IReadOnlyList<Turno>>(consulta.OrderBy(t => t.Inicio).ToList());
    }

    public Task<bool> ExisteSolapeAsync(DateTime inicioUtc, DateTime finUtc, Guid? excluirId, CancellationToken ct) =>
        Task.FromResult(_turnos.Any(t =>
            t.Estado != EstadoTurno.Cancelado
            && t.Inicio < finUtc
            && inicioUtc < t.Fin
            && (excluirId is null || t.Id != excluirId)));

    public Task AgregarAsync(Turno turno, CancellationToken ct)
    {
        _turnos.Add(turno);
        return Task.CompletedTask;
    }

    public Task GuardarCambiosAsync(CancellationToken ct)
    {
        Guardados++;
        return Task.CompletedTask;
    }
}
