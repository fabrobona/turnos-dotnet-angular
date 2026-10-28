using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Turnos.Application.Common;
using Turnos.Domain.Exceptions;

namespace Turnos.Api;

/// <summary>
/// Traduce las excepciones de la aplicación a respuestas HTTP uniformes (RFC 7807, ProblemDetails).
/// 400 validación · 404 no existe · 409 conflicto · 422 regla de negocio · 500 error inesperado.
/// </summary>
public class GlobalExceptionHandler(IProblemDetailsService problemas, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    private record Resultado(int Status, string Titulo, string? Detalle = null, Dictionary<string, string[]>? Errores = null);

    public async ValueTask<bool> TryHandleAsync(HttpContext contexto, Exception ex, CancellationToken ct)
    {
        var resultado = ex switch
        {
            ValidationException v => new Resultado(400, "Los datos enviados no son válidos.", null, AgruparErrores(v)),
            NotFoundException => new Resultado(404, "No se encontró lo que buscabas.", ex.Message),
            ConflictException => new Resultado(409, "La operación entra en conflicto con el estado actual.", ex.Message),
            DomainException => new Resultado(422, "La operación incumple una regla de negocio.", ex.Message),
            _ => new Resultado(500, "Ocurrió un error inesperado.")
        };

        if (resultado.Status == 500)
            logger.LogError(ex, "Error no controlado en {Ruta}", contexto.Request.Path);

        contexto.Response.StatusCode = resultado.Status;

        var problema = new ProblemDetails
        {
            Status = resultado.Status,
            Title = resultado.Titulo,
            Detail = resultado.Detalle,
            Instance = contexto.Request.Path
        };
        if (resultado.Errores is not null) problema.Extensions["errors"] = resultado.Errores;

        return await problemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = problema,
            Exception = ex
        });
    }

    private static Dictionary<string, string[]> AgruparErrores(ValidationException ex) =>
        ex.Errors
            .GroupBy(e => Camel(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

    private static string Camel(string nombre) =>
        string.IsNullOrEmpty(nombre) ? nombre : char.ToLowerInvariant(nombre[0]) + nombre[1..];
}
