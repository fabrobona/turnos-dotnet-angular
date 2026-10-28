using FluentValidation;
using MediatR;
using Turnos.Application.Abstractions;
using Turnos.Application.Common;
using Turnos.Application.Dtos;
using Turnos.Domain.Entities;

namespace Turnos.Application.Turnos;

public record ReprogramarTurnoCommand(Guid Id, DateTimeOffset Inicio, int DuracionMinutos) : IRequest<TurnoDto>;

public class ReprogramarTurnoValidator : AbstractValidator<ReprogramarTurnoCommand>
{
    public ReprogramarTurnoValidator()
    {
        RuleFor(x => x.Inicio).NotEmpty();
        RuleFor(x => x.DuracionMinutos)
            .InclusiveBetween(Turno.DuracionMinimaMinutos, Turno.DuracionMaximaMinutos);
    }
}

public class ReprogramarTurnoHandler(ITurnoRepository repositorio, TimeProvider reloj)
    : IRequestHandler<ReprogramarTurnoCommand, TurnoDto>
{
    public async Task<TurnoDto> Handle(ReprogramarTurnoCommand request, CancellationToken ct)
    {
        var turno = await repositorio.ObtenerPorIdAsync(request.Id, ct)
            ?? throw new NotFoundException($"No existe el turno {request.Id}.");

        turno.Reprogramar(request.Inicio.UtcDateTime, request.DuracionMinutos, reloj.GetUtcNow().UtcDateTime);

        if (await repositorio.ExisteSolapeAsync(turno.Inicio, turno.Fin, turno.Id, ct))
            throw new ConflictException("Ya existe un turno en ese horario.");

        await repositorio.GuardarCambiosAsync(ct);

        return turno.ToDto();
    }
}
