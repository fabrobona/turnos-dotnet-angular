using FluentValidation;
using MediatR;
using Turnos.Application.Abstractions;
using Turnos.Application.Common;
using Turnos.Application.Dtos;
using Turnos.Domain.Entities;

namespace Turnos.Application.Turnos;

public record ObtenerTurnosQuery(DateTimeOffset? Desde, DateTimeOffset? Hasta, EstadoTurno? Estado)
    : IRequest<IReadOnlyList<TurnoDto>>;

public class ObtenerTurnosValidator : AbstractValidator<ObtenerTurnosQuery>
{
    public ObtenerTurnosValidator()
    {
        RuleFor(x => x)
            .Must(x => x.Desde is null || x.Hasta is null || x.Desde <= x.Hasta)
            .WithMessage("'desde' no puede ser posterior a 'hasta'.");
    }
}

public class ObtenerTurnosHandler(ITurnoRepository repositorio)
    : IRequestHandler<ObtenerTurnosQuery, IReadOnlyList<TurnoDto>>
{
    public async Task<IReadOnlyList<TurnoDto>> Handle(ObtenerTurnosQuery request, CancellationToken ct)
    {
        var turnos = await repositorio.ListarAsync(
            request.Desde?.UtcDateTime, request.Hasta?.UtcDateTime, request.Estado, ct);

        return turnos.Select(t => t.ToDto()).ToList();
    }
}

public record ObtenerTurnoQuery(Guid Id) : IRequest<TurnoDto>;

public class ObtenerTurnoHandler(ITurnoRepository repositorio) : IRequestHandler<ObtenerTurnoQuery, TurnoDto>
{
    public async Task<TurnoDto> Handle(ObtenerTurnoQuery request, CancellationToken ct)
    {
        var turno = await repositorio.ObtenerPorIdAsync(request.Id, ct)
            ?? throw new NotFoundException($"No existe el turno {request.Id}.");

        return turno.ToDto();
    }
}
