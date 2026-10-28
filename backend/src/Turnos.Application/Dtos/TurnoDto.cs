using Turnos.Domain.Entities;

namespace Turnos.Application.Dtos;

public record TurnoDto(
    Guid Id,
    string ClienteNombre,
    string? ClienteTelefono,
    string Servicio,
    DateTime Inicio,
    DateTime Fin,
    int DuracionMinutos,
    EstadoTurno Estado,
    string? Notas);

/// <summary>Datos propuestos a partir de un mensaje libre. Los campos que no se pudieron deducir quedan en null.</summary>
public record TurnoSugeridoDto(
    string? ClienteNombre,
    string? Servicio,
    DateTime? Inicio,
    int DuracionMinutos,
    string? Notas,
    string Origen);

public static class TurnoMapper
{
    public static TurnoDto ToDto(this Turno t) => new(
        t.Id, t.ClienteNombre, t.ClienteTelefono, t.Servicio,
        t.Inicio, t.Fin, t.DuracionMinutos, t.Estado, t.Notas);
}
