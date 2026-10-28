using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Turnos.Application.Dtos;
using Turnos.Domain.Entities;

namespace Turnos.IntegrationTests;

public class TurnosApiTests(TurnosApiFactory fabrica) : IClassFixture<TurnosApiFactory>
{
    private readonly HttpClient _http = fabrica.CreateClient();
    private static int _dia;

    // Cada test usa un día distinto para no chocar con los turnos de los demás.
    private static DateTime DiaLibre(int hora = 9, int minuto = 0) =>
        new DateTime(2026, 11, 2, 0, 0, 0, DateTimeKind.Utc).AddDays(10 + Interlocked.Increment(ref _dia))
            .AddHours(hora).AddMinutes(minuto);

    private static object Nuevo(DateTime inicio, int duracion = 30, string nombre = "Ana Pérez") => new
    {
        clienteNombre = nombre,
        clienteTelefono = "600 111 222",
        servicio = "Corte",
        inicio,
        duracionMinutos = duracion,
        notas = (string?)null
    };

    private async Task<TurnoDto> CrearAsync(DateTime inicio, int duracion = 30)
    {
        var respuesta = await _http.PostAsJsonAsync("/api/turnos", Nuevo(inicio, duracion));
        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await respuesta.Content.ReadFromJsonAsync<TurnoDto>(TurnosApiFactory.Json))!;
    }

    [Fact]
    public async Task Crear_devuelve_201_con_Location_y_el_turno_queda_pendiente()
    {
        var inicio = DiaLibre();

        var respuesta = await _http.PostAsJsonAsync("/api/turnos", Nuevo(inicio));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
        var turno = (await respuesta.Content.ReadFromJsonAsync<TurnoDto>(TurnosApiFactory.Json))!;
        turno.Estado.Should().Be(EstadoTurno.Pendiente);
        turno.Fin.Should().Be(inicio.AddMinutes(30));
        respuesta.Headers.Location!.AbsolutePath.Should().Be($"/api/turnos/{turno.Id}");

        var leido = await _http.GetFromJsonAsync<TurnoDto>(respuesta.Headers.Location, TurnosApiFactory.Json);
        leido!.Id.Should().Be(turno.Id);
    }

    [Fact]
    public async Task Las_fechas_viajan_en_UTC_con_la_Z()
    {
        var turno = await CrearAsync(DiaLibre(10));

        var crudo = await _http.GetStringAsync($"/api/turnos/{turno.Id}");

        using var json = JsonDocument.Parse(crudo);
        json.RootElement.GetProperty("inicio").GetString().Should().EndWith("Z");
        json.RootElement.GetProperty("fin").GetString().Should().EndWith("Z");
        json.RootElement.GetProperty("estado").GetString().Should().Be("Pendiente");
    }

    [Fact]
    public async Task Crear_en_un_horario_ocupado_devuelve_409()
    {
        var inicio = DiaLibre();
        await CrearAsync(inicio, 60);

        var respuesta = await _http.PostAsJsonAsync("/api/turnos", Nuevo(inicio.AddMinutes(30)));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        problema.GetProperty("status").GetInt32().Should().Be(409);
        problema.GetProperty("detail").GetString().Should().Contain("horario");
    }

    [Fact]
    public async Task Un_turno_contiguo_no_es_conflicto()
    {
        var inicio = DiaLibre();
        await CrearAsync(inicio, 30);

        var respuesta = await _http.PostAsJsonAsync("/api/turnos", Nuevo(inicio.AddMinutes(30)));

        respuesta.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Cancelar_libera_el_horario()
    {
        var inicio = DiaLibre();
        var turno = await CrearAsync(inicio);

        (await _http.PostAsync($"/api/turnos/{turno.Id}/cancelar", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        var nuevo = await _http.PostAsJsonAsync("/api/turnos", Nuevo(inicio));

        nuevo.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Crear_en_el_pasado_devuelve_422()
    {
        var respuesta = await _http.PostAsJsonAsync("/api/turnos", Nuevo(new DateTime(2026, 11, 1, 9, 0, 0, DateTimeKind.Utc)));

        respuesta.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task Datos_invalidos_devuelven_400_con_los_errores_por_campo()
    {
        var respuesta = await _http.PostAsJsonAsync("/api/turnos", new
        {
            clienteNombre = "",
            clienteTelefono = "abc",
            servicio = "Corte",
            inicio = DiaLibre(),
            duracionMinutos = 0
        });

        respuesta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problema = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
        var errores = problema.GetProperty("errors");
        errores.TryGetProperty("clienteNombre", out _).Should().BeTrue();
        errores.TryGetProperty("clienteTelefono", out _).Should().BeTrue();
        errores.TryGetProperty("duracionMinutos", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Un_turno_inexistente_devuelve_404()
    {
        var respuesta = await _http.GetAsync($"/api/turnos/{Guid.NewGuid()}");

        respuesta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Un_id_que_no_es_guid_devuelve_404()
    {
        (await _http.GetAsync("/api/turnos/no-es-un-guid")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reprogramar_mueve_el_turno_y_respeta_los_conflictos()
    {
        var dia = DiaLibre(0);
        await CrearAsync(dia.AddHours(9), 60);              // 9:00 - 10:00
        var b = await CrearAsync(dia.AddHours(12), 30);     // 12:00 - 12:30

        var ocupado = await _http.PutAsJsonAsync($"/api/turnos/{b.Id}/horario",
            new { inicio = dia.AddHours(9).AddMinutes(30), duracionMinutos = 30 });
        ocupado.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var libre = await _http.PutAsJsonAsync($"/api/turnos/{b.Id}/horario",
            new { inicio = dia.AddHours(14), duracionMinutos = 45 });
        libre.StatusCode.Should().Be(HttpStatusCode.OK);
        var movido = (await libre.Content.ReadFromJsonAsync<TurnoDto>(TurnosApiFactory.Json))!;
        movido.Inicio.Should().Be(dia.AddHours(14));
        movido.Fin.Should().Be(dia.AddHours(14).AddMinutes(45));
    }

    [Fact]
    public async Task Confirmar_dos_veces_devuelve_422()
    {
        var turno = await CrearAsync(DiaLibre());

        (await _http.PostAsync($"/api/turnos/{turno.Id}/confirmar", null)).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _http.PostAsync($"/api/turnos/{turno.Id}/confirmar", null)).StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task Listar_filtra_por_estado_y_por_rango_y_ordena_por_fecha()
    {
        var dia = DiaLibre(0);
        var tarde = await CrearAsync(dia.AddHours(15));
        var temprano = await CrearAsync(dia.AddHours(9));
        await _http.PostAsync($"/api/turnos/{tarde.Id}/cancelar", null);

        var desde = Uri.EscapeDataString(dia.ToString("o"));
        var hasta = Uri.EscapeDataString(dia.AddDays(1).ToString("o"));

        var delDia = await _http.GetFromJsonAsync<List<TurnoDto>>($"/api/turnos?desde={desde}&hasta={hasta}", TurnosApiFactory.Json);
        delDia!.Select(t => t.Id).Should().Equal(temprano.Id, tarde.Id);

        var cancelados = await _http.GetFromJsonAsync<List<TurnoDto>>(
            $"/api/turnos?desde={desde}&hasta={hasta}&estado=Cancelado", TurnosApiFactory.Json);
        cancelados!.Should().ContainSingle().Which.Id.Should().Be(tarde.Id);
    }

    [Fact]
    public async Task Un_estado_invalido_en_el_filtro_devuelve_400()
    {
        (await _http.GetAsync("/api/turnos?estado=Inventado")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Interpretar_un_mensaje_devuelve_una_sugerencia_sin_crear_nada()
    {
        var antes = (await _http.GetFromJsonAsync<List<TurnoDto>>("/api/turnos", TurnosApiFactory.Json))!.Count;

        var respuesta = await _http.PostAsJsonAsync("/api/turnos/interpretar-mensaje",
            new { mensaje = "Hola, soy Ana García, quiero un corte mañana a las 15" });

        respuesta.StatusCode.Should().Be(HttpStatusCode.OK);
        var sugerencia = (await respuesta.Content.ReadFromJsonAsync<TurnoSugeridoDto>(TurnosApiFactory.Json))!;
        sugerencia.ClienteNombre.Should().Be("Ana García");
        sugerencia.Servicio.Should().Be("Corte");
        sugerencia.Inicio.Should().Be(new DateTime(2026, 11, 3, 15, 0, 0, DateTimeKind.Utc));
        sugerencia.Origen.Should().Be("reglas");

        var despues = (await _http.GetFromJsonAsync<List<TurnoDto>>("/api/turnos", TurnosApiFactory.Json))!.Count;
        despues.Should().Be(antes);
    }

    [Fact]
    public async Task Interpretar_un_mensaje_vacio_o_gigante_devuelve_400()
    {
        (await _http.PostAsJsonAsync("/api/turnos/interpretar-mensaje", new { mensaje = "" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await _http.PostAsJsonAsync("/api/turnos/interpretar-mensaje", new { mensaje = new string('a', 1001) }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Health_y_swagger_responden()
    {
        (await _http.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await _http.GetAsync("/swagger/v1/swagger.json")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
