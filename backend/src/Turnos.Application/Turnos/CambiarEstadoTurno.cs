using MediatR;
using Turnos.Application.Abstractions;
using Turnos.Application.Common;
using Turnos.Application.Dtos;

namespace Turnos.Application.Turnos;

public enum AccionTurno
{
    Confirmar,
    Completar,
    Cancelar
}

public record CambiarEstadoTurnoCommand(Guid Id, AccionTurno Accion) : IRequest<TurnoDto>;

public class CambiarEstadoTurnoHandler(ITurnoRepository repositorio, TimeProvider reloj)
    : IRequestHandler<CambiarEstadoTurnoCommand, TurnoDto>
{
    public async Task<TurnoDto> Handle(CambiarEstadoTurnoCommand request, CancellationToken ct)
    {
        var turno = await repositorio.ObtenerPorIdAsync(request.Id, ct)
            ?? throw new NotFoundException($"No existe el turno {request.Id}.");

        switch (request.Accion)
        {
            case AccionTurno.Confirmar:
                turno.Confirmar();
                break;
            case AccionTurno.Completar:
                turno.Completar(reloj.GetUtcNow().UtcDateTime);
                break;
            case AccionTurno.Cancelar:
                turno.Cancelar();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request), request.Accion, "Acción desconocida.");
        }

        await repositorio.GuardarCambiosAsync(ct);

        return turno.ToDto();
    }
}
