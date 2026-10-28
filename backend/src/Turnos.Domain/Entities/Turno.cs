using Turnos.Domain.Exceptions;

namespace Turnos.Domain.Entities;

public enum EstadoTurno
{
    Pendiente = 0,
    Confirmado = 1,
    Completado = 2,
    Cancelado = 3
}

/// <summary>
/// Turno agendado con un cliente. Todas las fechas se guardan en UTC.
/// Las reglas de negocio viven aquí y no en los controladores ni en los handlers.
/// </summary>
public class Turno
{
    public const int DuracionMinimaMinutos = 5;
    public const int DuracionMaximaMinutos = 480;

    public Guid Id { get; private set; }
    public string ClienteNombre { get; private set; } = string.Empty;
    public string? ClienteTelefono { get; private set; }
    public string Servicio { get; private set; } = string.Empty;
    public DateTime Inicio { get; private set; }
    public DateTime Fin { get; private set; }
    public int DuracionMinutos { get; private set; }
    public EstadoTurno Estado { get; private set; }
    public string? Notas { get; private set; }
    public DateTime CreadoEn { get; private set; }

    private Turno() { }

    public static Turno Crear(
        string clienteNombre,
        string? clienteTelefono,
        string servicio,
        DateTime inicioUtc,
        int duracionMinutos,
        string? notas,
        DateTime ahoraUtc)
    {
        if (string.IsNullOrWhiteSpace(clienteNombre))
            throw new DomainException("El nombre del cliente es obligatorio.");
        if (string.IsNullOrWhiteSpace(servicio))
            throw new DomainException("El servicio es obligatorio.");

        ValidarHorario(inicioUtc, duracionMinutos, ahoraUtc);

        return new Turno
        {
            Id = Guid.NewGuid(),
            ClienteNombre = clienteNombre.Trim(),
            ClienteTelefono = string.IsNullOrWhiteSpace(clienteTelefono) ? null : clienteTelefono.Trim(),
            Servicio = servicio.Trim(),
            Inicio = inicioUtc,
            DuracionMinutos = duracionMinutos,
            Fin = inicioUtc.AddMinutes(duracionMinutos),
            Estado = EstadoTurno.Pendiente,
            Notas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim(),
            CreadoEn = ahoraUtc
        };
    }

    public void Reprogramar(DateTime nuevoInicioUtc, int nuevaDuracionMinutos, DateTime ahoraUtc)
    {
        if (Estado is EstadoTurno.Completado or EstadoTurno.Cancelado)
            throw new DomainException($"No se puede reprogramar un turno {Estado.ToString().ToLowerInvariant()}.");

        ValidarHorario(nuevoInicioUtc, nuevaDuracionMinutos, ahoraUtc);

        Inicio = nuevoInicioUtc;
        DuracionMinutos = nuevaDuracionMinutos;
        Fin = nuevoInicioUtc.AddMinutes(nuevaDuracionMinutos);
    }

    public void Confirmar()
    {
        if (Estado != EstadoTurno.Pendiente)
            throw new DomainException("Solo se puede confirmar un turno pendiente.");

        Estado = EstadoTurno.Confirmado;
    }

    public void Completar(DateTime ahoraUtc)
    {
        if (Estado != EstadoTurno.Confirmado)
            throw new DomainException("Solo se puede completar un turno confirmado.");
        if (Inicio > ahoraUtc)
            throw new DomainException("No se puede completar un turno que todavía no empezó.");

        Estado = EstadoTurno.Completado;
    }

    public void Cancelar()
    {
        if (Estado is EstadoTurno.Completado or EstadoTurno.Cancelado)
            throw new DomainException($"No se puede cancelar un turno {Estado.ToString().ToLowerInvariant()}.");

        Estado = EstadoTurno.Cancelado;
    }

    private static void ValidarHorario(DateTime inicioUtc, int duracionMinutos, DateTime ahoraUtc)
    {
        if (duracionMinutos is < DuracionMinimaMinutos or > DuracionMaximaMinutos)
            throw new DomainException(
                $"La duración debe estar entre {DuracionMinimaMinutos} y {DuracionMaximaMinutos} minutos.");
        if (inicioUtc <= ahoraUtc)
            throw new DomainException("El turno debe ser en el futuro.");
    }
}
