using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Turnos.IntegrationTests;

/// <summary>
/// Levanta la API real con una base SQLite en un archivo temporal y un reloj controlable.
/// Cada instancia usa su propia base, así los tests no se pisan entre sí.
/// </summary>
public class TurnosApiFactory : WebApplicationFactory<Program>
{
    private readonly string _rutaDb = Path.Combine(Path.GetTempPath(), $"turnos-tests-{Guid.NewGuid():N}.db");

    // Lunes 2 de noviembre de 2026, 08:00 UTC.
    public FakeTimeProvider Reloj { get; } = new(new DateTimeOffset(2026, 11, 2, 8, 0, 0, TimeSpan.Zero));

    public static JsonSerializerOptions Json { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_rutaDb}");
        builder.UseSetting("Turnos:CargarDatosDeEjemplo", "false");
        builder.UseSetting("Turnos:ZonaHoraria", "UTC");

        builder.ConfigureTestServices(servicios =>
        {
            servicios.RemoveAll<TimeProvider>();
            servicios.AddSingleton<TimeProvider>(Reloj);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing) return;

        SqliteConnection.ClearAllPools();
        foreach (var archivo in new[] { _rutaDb, _rutaDb + "-wal", _rutaDb + "-shm" })
        {
            try { File.Delete(archivo); } catch (IOException) { /* limpieza best-effort */ }
        }
    }
}
