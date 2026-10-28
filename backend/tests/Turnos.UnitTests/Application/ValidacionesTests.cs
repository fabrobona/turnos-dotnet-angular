using FluentAssertions;
using FluentValidation;
using MediatR;
using Turnos.Application.Behaviors;
using Turnos.Application.Turnos;

namespace Turnos.UnitTests.Application;

public class ValidacionesTests
{
    private static CrearTurnoCommand ComandoValido() => new(
        "Ana", "+34 600 111 222", "Corte", DateTimeOffset.UtcNow.AddDays(1), 30, null);

    [Fact]
    public void Un_comando_correcto_es_valido()
    {
        new CrearTurnoValidator().Validate(ComandoValido()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "Corte", "600", 30, nameof(CrearTurnoCommand.ClienteNombre))]
    [InlineData("Ana", "", "600", 30, nameof(CrearTurnoCommand.Servicio))]
    [InlineData("Ana", "Corte", "abc", 30, nameof(CrearTurnoCommand.ClienteTelefono))]
    [InlineData("Ana", "Corte", "600", 0, nameof(CrearTurnoCommand.DuracionMinutos))]
    [InlineData("Ana", "Corte", "600", 481, nameof(CrearTurnoCommand.DuracionMinutos))]
    public void Reporta_el_campo_invalido(string nombre, string servicio, string telefono, int duracion, string campo)
    {
        var comando = ComandoValido() with
        {
            ClienteNombre = nombre, Servicio = servicio, ClienteTelefono = telefono, DuracionMinutos = duracion
        };

        var resultado = new CrearTurnoValidator().Validate(comando);

        resultado.Errors.Select(e => e.PropertyName).Should().Contain(campo);
    }

    [Fact]
    public void El_rango_de_fechas_no_puede_estar_invertido()
    {
        var ahora = DateTimeOffset.UtcNow;

        new ObtenerTurnosValidator().Validate(new ObtenerTurnosQuery(ahora, ahora.AddDays(-1), null))
            .IsValid.Should().BeFalse();
        new ObtenerTurnosValidator().Validate(new ObtenerTurnosQuery(ahora, ahora.AddDays(1), null))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public void El_mensaje_a_interpretar_tiene_largo_maximo()
    {
        var validador = new InterpretarMensajeValidator();

        validador.Validate(new InterpretarMensajeQuery(new string('a', InterpretarMensajeValidator.LargoMaximo)))
            .IsValid.Should().BeTrue();
        validador.Validate(new InterpretarMensajeQuery(new string('a', InterpretarMensajeValidator.LargoMaximo + 1)))
            .IsValid.Should().BeFalse();
        validador.Validate(new InterpretarMensajeQuery("")).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task El_pipeline_corta_antes_del_handler_si_hay_errores()
    {
        var comportamiento = new ValidationBehavior<CrearTurnoCommand, Unit>([new CrearTurnoValidator()]);
        var handlerEjecutado = false;

        var accion = () => comportamiento.Handle(
            ComandoValido() with { ClienteNombre = "" },
            () =>
            {
                handlerEjecutado = true;
                return Task.FromResult(Unit.Value);
            },
            default);

        await accion.Should().ThrowAsync<ValidationException>();
        handlerEjecutado.Should().BeFalse();
    }

    [Fact]
    public async Task El_pipeline_deja_pasar_los_comandos_validos()
    {
        var comportamiento = new ValidationBehavior<CrearTurnoCommand, Unit>([new CrearTurnoValidator()]);

        var resultado = await comportamiento.Handle(ComandoValido(), () => Task.FromResult(Unit.Value), default);

        resultado.Should().Be(Unit.Value);
    }
}
