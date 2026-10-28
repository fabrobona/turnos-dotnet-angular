using Turnos.Application.Dtos;

namespace Turnos.Application.Abstractions;

/// <summary>
/// Convierte un mensaje libre (por ejemplo de WhatsApp) en una sugerencia de turno.
/// Nunca crea nada: solo propone datos para que una persona los confirme.
/// </summary>
public interface IInterpretadorMensajes
{
    Task<TurnoSugeridoDto> InterpretarAsync(string mensaje, DateTime ahoraUtc, CancellationToken ct);
}
