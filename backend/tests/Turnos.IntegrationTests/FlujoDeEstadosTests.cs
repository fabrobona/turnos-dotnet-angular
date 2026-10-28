using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Turnos.Application.Dtos;
using Turnos.Domain.Entities;

namespace Turnos.IntegrationTests;

/// <summary>
/// Vive aparte porque adelanta el reloj, y eso afectaría a otros tests si compartieran fábrica.
/// </summary>
public class FlujoDeEstadosTests(TurnosApiFactory fabrica) : IClassFixture<TurnosApiFactory>
{
    private readonly HttpClient _http = fabrica.CreateClient();

    [Fact]
    public async Task Un_turno_se_confirma_y_solo_se_completa_cuando_ya_empezo()
    {
        var inicio = new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
        var creado = await _http.PostAsJsonAsync("/api/turnos", new
        {
            clienteNombre = "Marta", servicio = "Corte", inicio, duracionMinutos = 30
        });
        var turno = (await creado.Content.ReadFromJsonAsync<TurnoDto>(TurnosApiFactory.Json))!;

        // No se puede completar sin confirmar.
        (await _http.PostAsync($"/api/turnos/{turno.Id}/completar", null)).StatusCode.Should().Be((HttpStatusCode)422);

        (await _http.PostAsync($"/api/turnos/{turno.Id}/confirmar", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        // Confirmado, pero todavía no empezó.
        (await _http.PostAsync($"/api/turnos/{turno.Id}/completar", null)).StatusCode.Should().Be((HttpStatusCode)422);

        // Pasa el tiempo.
        fabrica.Reloj.SetUtcNow(new DateTimeOffset(inicio.AddMinutes(10), TimeSpan.Zero));

        var completado = await _http.PostAsync($"/api/turnos/{turno.Id}/completar", null);
        completado.StatusCode.Should().Be(HttpStatusCode.OK);
        (await completado.Content.ReadFromJsonAsync<TurnoDto>(TurnosApiFactory.Json))!.Estado
            .Should().Be(EstadoTurno.Completado);

        // Un turno completado ya no se puede cancelar.
        (await _http.PostAsync($"/api/turnos/{turno.Id}/cancelar", null)).StatusCode.Should().Be((HttpStatusCode)422);
    }
}
