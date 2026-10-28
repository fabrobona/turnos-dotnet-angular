using FluentAssertions;
using Turnos.Application.Common;
using Turnos.Application.Interpretacion;

namespace Turnos.UnitTests.Interpretacion;

public class ReglasInterpretadorTests
{
    // Lunes 2 de noviembre de 2026, 10:00 UTC.
    private static readonly DateTime Ahora = new(2026, 11, 2, 10, 0, 0, DateTimeKind.Utc);

    private static readonly ReglasInterpretador EnUtc = new(new ZonaHorariaLocal(TimeZoneInfo.Utc));

    private static DateTime Utc(int y, int mo, int d, int h, int mi = 0) =>
        new(y, mo, d, h, mi, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Entiende_nombre_servicio_y_fecha_relativa()
    {
        var r = await EnUtc.InterpretarAsync("Hola, soy Ana García, quiero un corte mañana a las 15", Ahora, default);

        r.ClienteNombre.Should().Be("Ana García");
        r.Servicio.Should().Be("Corte");
        r.Inicio.Should().Be(Utc(2026, 11, 3, 15));
        r.Origen.Should().Be("reglas");
        r.DuracionMinutos.Should().Be(ReglasInterpretador.DuracionPorDefectoMinutos);
    }

    [Theory]
    [InlineData("turno el jueves a las 4 de la tarde", 2026, 11, 5, 16, 0)]
    [InlineData("el lunes a las 10", 2026, 11, 9, 10, 0)]
    [InlineData("el miércoles a las 9:30", 2026, 11, 4, 9, 30)]
    [InlineData("pasado mañana a las 18:30", 2026, 11, 4, 18, 30)]
    [InlineData("hoy a las 17", 2026, 11, 2, 17, 0)]
    [InlineData("mañana por la mañana a las 9", 2026, 11, 3, 9, 0)]
    [InlineData("12/11 a las 10:30", 2026, 11, 12, 10, 30)]
    [InlineData("el 15 de noviembre a las 9 de la mañana", 2026, 11, 15, 9, 0)]
    [InlineData("el sábado a las 11h", 2026, 11, 7, 11, 0)]
    public async Task Interpreta_fechas_y_horas_en_espanol(string mensaje, int y, int mo, int d, int h, int mi)
    {
        var r = await EnUtc.InterpretarAsync(mensaje, Ahora, default);

        r.Inicio.Should().Be(Utc(y, mo, d, h, mi));
    }

    [Fact]
    public async Task El_mismo_dia_de_la_semana_apunta_a_la_semana_siguiente()
    {
        var r = await EnUtc.InterpretarAsync("el lunes a las 10", Ahora, default);

        r.Inicio!.Value.Date.Should().Be(new DateTime(2026, 11, 9));
    }

    [Fact]
    public async Task Una_fecha_sin_anio_que_ya_paso_se_asume_del_anio_siguiente()
    {
        var r = await EnUtc.InterpretarAsync("el 10/01 a las 10", Ahora, default);

        r.Inicio.Should().Be(Utc(2027, 1, 10, 10));
    }

    [Fact]
    public async Task Una_hora_entre_la_1_y_las_7_sin_aclarar_se_asume_de_la_tarde()
    {
        var r = await EnUtc.InterpretarAsync("mañana a las 5", Ahora, default);

        r.Inicio.Should().Be(Utc(2026, 11, 3, 17));
    }

    [Fact]
    public async Task Si_falta_la_hora_o_la_fecha_no_inventa_el_inicio()
    {
        (await EnUtc.InterpretarAsync("quiero un turno mañana", Ahora, default)).Inicio.Should().BeNull();
        (await EnUtc.InterpretarAsync("quiero un turno a las 15", Ahora, default)).Inicio.Should().BeNull();
    }

    [Fact]
    public async Task Un_mensaje_sin_datos_devuelve_todo_vacio()
    {
        var r = await EnUtc.InterpretarAsync("hola, ¿cómo estás?", Ahora, default);

        r.ClienteNombre.Should().BeNull();
        r.Servicio.Should().BeNull();
        r.Inicio.Should().BeNull();
    }

    [Fact]
    public async Task Rechaza_fechas_imposibles_sin_romperse()
    {
        var r = await EnUtc.InterpretarAsync("el 31/02 a las 10", Ahora, default);

        r.Inicio.Should().BeNull();
    }

    [Fact]
    public async Task Convierte_la_hora_local_de_Madrid_a_UTC()
    {
        var madrid = new ReglasInterpretador(ZonaHorariaLocal.Desde("Europe/Madrid"));

        // El 3 de noviembre de 2026 Madrid está en horario de invierno (UTC+1).
        var r = await madrid.InterpretarAsync("mañana a las 15", Ahora, default);

        r.Inicio.Should().Be(Utc(2026, 11, 3, 14));
    }

    [Fact]
    public void Una_zona_horaria_desconocida_cae_en_UTC()
    {
        ZonaHorariaLocal.Desde("Inventada/Zona").Zona.Should().Be(TimeZoneInfo.Utc);
    }
}
