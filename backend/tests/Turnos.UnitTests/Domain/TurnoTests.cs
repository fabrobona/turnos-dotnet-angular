using FluentAssertions;
using Turnos.Domain.Entities;
using Turnos.Domain.Exceptions;

namespace Turnos.UnitTests.Domain;

public class TurnoTests
{
    private static readonly DateTime Ahora = new(2026, 11, 2, 8, 0, 0, DateTimeKind.Utc);

    private static Turno NuevoTurno(DateTime? inicio = null, int duracion = 30) =>
        Turno.Crear("Ana Pérez", "600 111 222", "Corte", inicio ?? Ahora.AddDays(1), duracion, null, Ahora);

    [Fact]
    public void Crear_con_datos_validos_queda_pendiente_y_calcula_el_fin()
    {
        var inicio = Ahora.AddDays(1);

        var turno = NuevoTurno(inicio, 45);

        turno.Estado.Should().Be(EstadoTurno.Pendiente);
        turno.Inicio.Should().Be(inicio);
        turno.Fin.Should().Be(inicio.AddMinutes(45));
        turno.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Crear_recorta_espacios_y_convierte_textos_vacios_en_null()
    {
        var turno = Turno.Crear("  Ana  ", "   ", "  Corte ", Ahora.AddDays(1), 30, " ", Ahora);

        turno.ClienteNombre.Should().Be("Ana");
        turno.Servicio.Should().Be("Corte");
        turno.ClienteTelefono.Should().BeNull();
        turno.Notas.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(481)]
    public void Crear_con_duracion_fuera_de_rango_falla(int duracion)
    {
        var accion = () => NuevoTurno(duracion: duracion);

        accion.Should().Throw<DomainException>().WithMessage("*duración*");
    }

    [Theory]
    [InlineData(5)]
    [InlineData(480)]
    public void Crear_acepta_los_limites_de_duracion(int duracion)
    {
        NuevoTurno(duracion: duracion).DuracionMinutos.Should().Be(duracion);
    }

    [Fact]
    public void Crear_en_el_pasado_falla()
    {
        var accion = () => NuevoTurno(inicio: Ahora.AddMinutes(-1));

        accion.Should().Throw<DomainException>().WithMessage("*futuro*");
    }

    [Theory]
    [InlineData("", "Corte")]
    [InlineData("  ", "Corte")]
    [InlineData("Ana", "")]
    public void Crear_sin_nombre_o_sin_servicio_falla(string nombre, string servicio)
    {
        var accion = () => Turno.Crear(nombre, null, servicio, Ahora.AddDays(1), 30, null, Ahora);

        accion.Should().Throw<DomainException>();
    }

    [Fact]
    public void Confirmar_pasa_de_pendiente_a_confirmado_y_no_se_puede_repetir()
    {
        var turno = NuevoTurno();

        turno.Confirmar();

        turno.Estado.Should().Be(EstadoTurno.Confirmado);
        turno.Invoking(t => t.Confirmar()).Should().Throw<DomainException>();
    }

    [Fact]
    public void Completar_exige_estar_confirmado()
    {
        var turno = NuevoTurno();

        turno.Invoking(t => t.Completar(Ahora.AddDays(2))).Should().Throw<DomainException>()
            .WithMessage("*confirmado*");
    }

    [Fact]
    public void Completar_antes_de_que_empiece_falla_y_despues_funciona()
    {
        var turno = NuevoTurno(Ahora.AddDays(1));
        turno.Confirmar();

        turno.Invoking(t => t.Completar(Ahora)).Should().Throw<DomainException>().WithMessage("*empezó*");

        turno.Completar(turno.Inicio.AddMinutes(1));
        turno.Estado.Should().Be(EstadoTurno.Completado);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Cancelar_funciona_desde_pendiente_y_desde_confirmado(bool confirmar)
    {
        var turno = NuevoTurno();
        if (confirmar) turno.Confirmar();

        turno.Cancelar();

        turno.Estado.Should().Be(EstadoTurno.Cancelado);
    }

    [Fact]
    public void No_se_puede_cancelar_dos_veces_ni_un_turno_completado()
    {
        var cancelado = NuevoTurno();
        cancelado.Cancelar();
        cancelado.Invoking(t => t.Cancelar()).Should().Throw<DomainException>();

        var completado = NuevoTurno();
        completado.Confirmar();
        completado.Completar(completado.Inicio.AddMinutes(1));
        completado.Invoking(t => t.Cancelar()).Should().Throw<DomainException>();
    }

    [Fact]
    public void Reprogramar_actualiza_inicio_duracion_y_fin()
    {
        var turno = NuevoTurno();
        var nuevoInicio = Ahora.AddDays(3);

        turno.Reprogramar(nuevoInicio, 60, Ahora);

        turno.Inicio.Should().Be(nuevoInicio);
        turno.DuracionMinutos.Should().Be(60);
        turno.Fin.Should().Be(nuevoInicio.AddMinutes(60));
    }

    [Fact]
    public void No_se_puede_reprogramar_un_turno_cancelado_ni_llevarlo_al_pasado()
    {
        var turno = NuevoTurno();
        turno.Invoking(t => t.Reprogramar(Ahora.AddHours(-1), 30, Ahora)).Should().Throw<DomainException>();

        turno.Cancelar();
        turno.Invoking(t => t.Reprogramar(Ahora.AddDays(2), 30, Ahora)).Should().Throw<DomainException>()
            .WithMessage("*cancelado*");
    }
}
