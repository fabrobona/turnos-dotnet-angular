using FluentValidation;
using MediatR;
using Turnos.Application.Abstractions;
using Turnos.Application.Dtos;

namespace Turnos.Application.Turnos;

public record InterpretarMensajeQuery(string Mensaje) : IRequest<TurnoSugeridoDto>;

public class InterpretarMensajeValidator : AbstractValidator<InterpretarMensajeQuery>
{
    public const int LargoMaximo = 1000;

    public InterpretarMensajeValidator()
    {
        RuleFor(x => x.Mensaje).NotEmpty().MaximumLength(LargoMaximo);
    }
}

public class InterpretarMensajeHandler(IInterpretadorMensajes interpretador, TimeProvider reloj)
    : IRequestHandler<InterpretarMensajeQuery, TurnoSugeridoDto>
{
    public Task<TurnoSugeridoDto> Handle(InterpretarMensajeQuery request, CancellationToken ct) =>
        interpretador.InterpretarAsync(request.Mensaje, reloj.GetUtcNow().UtcDateTime, ct);
}
