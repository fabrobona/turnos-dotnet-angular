using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Turnos.Application.Common;
using Turnos.Application.Turnos;
using Turnos.Domain.Entities;
using Turnos.Domain.Exceptions;

namespace Turnos.UnitTests.Application;

public class CasosDeUsoTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 11, 2, 8, 0, 0, TimeSpan.Zero);

    private readonly RepositorioEnMemoria _repo = new();
    private readonly FakeTimeProvider _reloj = new(Ahora);

    private Task<Turno> AgendarAsync(int diasAdelante = 1, int hora = 9, int duracion = 30)
    {
        var turno = Turno.Crear("Ana", null, "Corte",
            Ahora.UtcDateTime.Date.AddDays(diasAdelante).AddHours(hora), duracion, null, Ahora.UtcDateTime);
        return _repo.AgregarAsync(turno, default).ContinueWith(_ => turno);
    }

    private CrearTurnoCommand Crear(int diasAdelante = 1, int hora = 9, int duracion = 30) => new(
        "Luis", null, "Barba",
        new DateTimeOffset(Ahora.UtcDateTime.Date.AddDays(diasAdelante).AddHours(hora), TimeSpan.Zero),
        duracion, null);

    [Fact]
    public async Task Crear_guarda_el_turno_y_devuelve_el_dto()
    {
        var dto = await new CrearTurnoHandler(_repo, _reloj).Handle(Crear(), default);

        dto.Estado.Should().Be(EstadoTurno.Pendiente);
        dto.ClienteNombre.Should().Be("Luis");
        _repo.Todos.Should().ContainSingle();
        _repo.Guardados.Should().Be(1);
    }

    [Fact]
    public async Task Crear_en_un_horario_ocupado_lanza_conflicto_y_no_guarda()
    {
        await AgendarAsync(hora: 9, duracion: 60);

        var accion = () => new CrearTurnoHandler(_repo, _reloj).Handle(Crear(hora: 9), default);

        await accion.Should().ThrowAsync<ConflictException>();
        _repo.Todos.Should().ContainSingle();
        _repo.Guardados.Should().Be(0);
    }

    [Fact]
    public async Task Un_turno_que_empieza_justo_cuando_termina_otro_no_es_conflicto()
    {
        await AgendarAsync(hora: 9, duracion: 60); // 9:00 - 10:00

        var dto = await new CrearTurnoHandler(_repo, _reloj).Handle(Crear(hora: 10), default);

        dto.Should().NotBeNull();
        _repo.Todos.Should().HaveCount(2);
    }

    [Fact]
    public async Task Un_turno_cancelado_libera_el_horario()
    {
        var existente = await AgendarAsync(hora: 9);
        existente.Cancelar();

        var dto = await new CrearTurnoHandler(_repo, _reloj).Handle(Crear(hora: 9), default);

        dto.Should().NotBeNull();
    }

    [Fact]
    public async Task Crear_en_el_pasado_lanza_excepcion_de_dominio()
    {
        var accion = () => new CrearTurnoHandler(_repo, _reloj).Handle(Crear(diasAdelante: -1), default);

        await accion.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Reprogramar_dentro_de_su_propio_horario_no_cuenta_como_conflicto()
    {
        var turno = await AgendarAsync(hora: 9, duracion: 60);
        var nuevoInicio = new DateTimeOffset(turno.Inicio.AddMinutes(15), TimeSpan.Zero);

        var dto = await new ReprogramarTurnoHandler(_repo, _reloj)
            .Handle(new ReprogramarTurnoCommand(turno.Id, nuevoInicio, 60), default);

        dto.Inicio.Should().Be(nuevoInicio.UtcDateTime);
    }

    [Fact]
    public async Task Reprogramar_sobre_otro_turno_lanza_conflicto()
    {
        await AgendarAsync(hora: 9, duracion: 60);
        var otro = await AgendarAsync(hora: 12, duracion: 30);
        var haciaLas9 = new DateTimeOffset(otro.Inicio.Date.AddHours(9).AddMinutes(30), TimeSpan.Zero);

        var accion = () => new ReprogramarTurnoHandler(_repo, _reloj)
            .Handle(new ReprogramarTurnoCommand(otro.Id, haciaLas9, 30), default);

        await accion.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Operar_sobre_un_turno_inexistente_lanza_not_found()
    {
        var id = Guid.NewGuid();

        await new Func<Task>(() => new ObtenerTurnoHandler(_repo).Handle(new ObtenerTurnoQuery(id), default))
            .Should().ThrowAsync<NotFoundException>();
        await new Func<Task>(() => new CambiarEstadoTurnoHandler(_repo, _reloj)
                .Handle(new CambiarEstadoTurnoCommand(id, AccionTurno.Confirmar), default))
            .Should().ThrowAsync<NotFoundException>();
        await new Func<Task>(() => new ReprogramarTurnoHandler(_repo, _reloj)
                .Handle(new ReprogramarTurnoCommand(id, Ahora.AddDays(1), 30), default))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Cambiar_estado_aplica_la_transicion_y_guarda()
    {
        var turno = await AgendarAsync();
        var handler = new CambiarEstadoTurnoHandler(_repo, _reloj);

        var confirmado = await handler.Handle(new CambiarEstadoTurnoCommand(turno.Id, AccionTurno.Confirmar), default);
        confirmado.Estado.Should().Be(EstadoTurno.Confirmado);

        _reloj.SetUtcNow(new DateTimeOffset(turno.Inicio.AddMinutes(5), TimeSpan.Zero));
        var completado = await handler.Handle(new CambiarEstadoTurnoCommand(turno.Id, AccionTurno.Completar), default);
        completado.Estado.Should().Be(EstadoTurno.Completado);

        _repo.Guardados.Should().Be(2);
    }

    [Fact]
    public async Task Listar_filtra_por_estado_y_rango_y_ordena_por_fecha()
    {
        var tarde = await AgendarAsync(hora: 15);
        var temprano = await AgendarAsync(hora: 9);
        var otroDia = await AgendarAsync(diasAdelante: 3, hora: 9);
        tarde.Cancelar();
        var handler = new ObtenerTurnosHandler(_repo);

        var todos = await handler.Handle(new ObtenerTurnosQuery(null, null, null), default);
        todos.Select(t => t.Id).Should().Equal(temprano.Id, tarde.Id, otroDia.Id);

        var canceladosONo = await handler.Handle(new ObtenerTurnosQuery(null, null, EstadoTurno.Cancelado), default);
        canceladosONo.Should().ContainSingle(t => t.Id == tarde.Id);

        var soloManana = await handler.Handle(new ObtenerTurnosQuery(
            new DateTimeOffset(temprano.Inicio.Date, TimeSpan.Zero),
            new DateTimeOffset(temprano.Inicio.Date.AddDays(1), TimeSpan.Zero), null), default);
        soloManana.Should().HaveCount(2);
    }
}
