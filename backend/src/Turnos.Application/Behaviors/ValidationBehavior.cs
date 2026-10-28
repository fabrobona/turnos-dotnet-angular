using FluentValidation;
using MediatR;

namespace Turnos.Application.Behaviors;

/// <summary>Ejecuta los validadores de FluentValidation antes de cada handler.</summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var contexto = new ValidationContext<TRequest>(request);
            var resultados = await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(contexto, cancellationToken)));
            var errores = resultados.SelectMany(r => r.Errors).Where(e => e is not null).ToList();

            if (errores.Count != 0)
                throw new ValidationException(errores);
        }

        return await next();
    }
}
