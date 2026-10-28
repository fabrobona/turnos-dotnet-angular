using Microsoft.Extensions.Logging;
using Turnos.Application.Abstractions;
using Turnos.Application.Dtos;

namespace Turnos.Infrastructure.Interpretacion;

/// <summary>
/// Prueba primero el intérprete principal (IA) y, si falla por cualquier motivo, usa el de reglas.
/// Así la aplicación sigue funcionando aunque el servicio externo caiga o no haya clave configurada.
/// </summary>
public class InterpretadorConRespaldo(
    IInterpretadorMensajes principal,
    IInterpretadorMensajes respaldo,
    ILogger<InterpretadorConRespaldo> logger) : IInterpretadorMensajes
{
    public async Task<TurnoSugeridoDto> InterpretarAsync(string mensaje, DateTime ahoraUtc, CancellationToken ct)
    {
        try
        {
            return await principal.InterpretarAsync(mensaje, ahoraUtc, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // No se registra el mensaje del cliente: puede contener datos personales.
            logger.LogWarning("El intérprete principal falló ({Tipo}); se usan las reglas.", ex.GetType().Name);
            return await respaldo.InterpretarAsync(mensaje, ahoraUtc, ct);
        }
    }
}
