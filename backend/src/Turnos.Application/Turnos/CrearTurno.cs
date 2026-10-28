using FluentValidation;
using MediatR;
using Turnos.Application.Abstractions;
using Turnos.Application.Common;
using Turnos.Application.Dtos;
using Turnos.Domain.Entities;

namespace Turnos.Application.Turnos;

public record CrearTurnoCommand(
    string ClienteNombre,
    string? ClienteTelefono,
    string Servicio,
    DateTimeOffset Inicio,
    int DuracionMinutos,
    string? Notas) : IRequest<TurnoDto>;

public class CrearTurnoValidator : AbstractValidator<CrearTurnoCommand>
{
    public CrearTurnoValidator()
    {
        RuleFor(x => x.ClienteNombre).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ClienteTelefono).MaximumLength(30)
            .Matches(@"^[0-9+()\-\s]*$").WithMessage("El teléfono solo puede tener números, espacios y + - ( ).");
        RuleFor(x => x.Servicio).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Inicio).NotEmpty();
        RuleFor(x => x.DuracionMinutos)
            .InclusiveBetween(Turno.DuracionMinimaMinutos, Turno.DuracionMaximaMinutos);
        RuleFor(x => x.Notas).MaximumLength(500);
    }
}

public class CrearTurnoHandler(ITurnoRepository repositorio, TimeProvider reloj)
    : IRequestHandler<CrearTurnoCommand, TurnoDto>
{
    public async Task<TurnoDto> Handle(CrearTurnoCommand request, CancellationToken ct)
    {
        var ahora = reloj.GetUtcNow().UtcDateTime;

        var turno = Turno.Crear(
            request.ClienteNombre,
            request.ClienteTelefono,
            request.Servicio,
            request.Inicio.UtcDateTime,
            request.DuracionMinutos,
            request.Notas,
            ahora);

        if (await repositorio.ExisteSolapeAsync(turno.Inicio, turno.Fin, null, ct))
            throw new ConflictException("Ya existe un turno en ese horario.");

        await repositorio.AgregarAsync(turno, ct);
        await repositorio.GuardarCambiosAsync(ct);

        return turno.ToDto();
    }
}
