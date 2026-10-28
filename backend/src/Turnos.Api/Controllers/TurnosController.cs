using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Turnos.Application.Dtos;
using Turnos.Application.Turnos;
using Turnos.Domain.Entities;

namespace Turnos.Api.Controllers;

public record CrearTurnoRequest(
    string ClienteNombre,
    string? ClienteTelefono,
    string Servicio,
    DateTimeOffset Inicio,
    int DuracionMinutos,
    string? Notas);

public record ReprogramarTurnoRequest(DateTimeOffset Inicio, int DuracionMinutos);

public record InterpretarMensajeRequest(string Mensaje);

[ApiController]
[Route("api/turnos")]
[Produces("application/json")]
public class TurnosController(ISender mediator) : ControllerBase
{
    /// <summary>Lista los turnos, ordenados por fecha, con filtros opcionales.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TurnoDto>>> Listar(
        [FromQuery] DateTimeOffset? desde,
        [FromQuery] DateTimeOffset? hasta,
        [FromQuery] EstadoTurno? estado,
        CancellationToken ct) =>
        Ok(await mediator.Send(new ObtenerTurnosQuery(desde, hasta, estado), ct));

    /// <summary>Devuelve un turno por id.</summary>
    [HttpGet("{id:guid}", Name = nameof(Obtener))]
    public async Task<ActionResult<TurnoDto>> Obtener(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new ObtenerTurnoQuery(id), ct));

    /// <summary>Crea un turno. Responde 409 si el horario ya está ocupado.</summary>
    [HttpPost]
    public async Task<ActionResult<TurnoDto>> Crear(CrearTurnoRequest request, CancellationToken ct)
    {
        var turno = await mediator.Send(new CrearTurnoCommand(
            request.ClienteNombre, request.ClienteTelefono, request.Servicio,
            request.Inicio, request.DuracionMinutos, request.Notas), ct);

        return CreatedAtRoute(nameof(Obtener), new { id = turno.Id }, turno);
    }

    /// <summary>Cambia fecha, hora y duración de un turno.</summary>
    [HttpPut("{id:guid}/horario")]
    public async Task<ActionResult<TurnoDto>> Reprogramar(
        Guid id, ReprogramarTurnoRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(new ReprogramarTurnoCommand(id, request.Inicio, request.DuracionMinutos), ct));

    /// <summary>Pendiente → Confirmado.</summary>
    [HttpPost("{id:guid}/confirmar")]
    public async Task<ActionResult<TurnoDto>> Confirmar(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new CambiarEstadoTurnoCommand(id, AccionTurno.Confirmar), ct));

    /// <summary>Confirmado → Completado (solo si el turno ya empezó).</summary>
    [HttpPost("{id:guid}/completar")]
    public async Task<ActionResult<TurnoDto>> Completar(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new CambiarEstadoTurnoCommand(id, AccionTurno.Completar), ct));

    /// <summary>Pendiente o Confirmado → Cancelado. Libera el horario.</summary>
    [HttpPost("{id:guid}/cancelar")]
    public async Task<ActionResult<TurnoDto>> Cancelar(Guid id, CancellationToken ct) =>
        Ok(await mediator.Send(new CambiarEstadoTurnoCommand(id, AccionTurno.Cancelar), ct));

    /// <summary>
    /// Interpreta un mensaje libre ("soy Ana, quiero un corte mañana a las 15") y propone los datos del turno.
    /// No crea nada: la persona confirma desde el formulario.
    /// </summary>
    [HttpPost("interpretar-mensaje")]
    [EnableRateLimiting("interpretar")]
    public async Task<ActionResult<TurnoSugeridoDto>> InterpretarMensaje(
        InterpretarMensajeRequest request, CancellationToken ct) =>
        Ok(await mediator.Send(new InterpretarMensajeQuery(request.Mensaje), ct));
}
