using Microsoft.EntityFrameworkCore;
using Turnos.Domain.Entities;

namespace Turnos.Infrastructure.Persistence;

/// <summary>Datos ficticios para poder probar la aplicación en desarrollo.</summary>
public static class DatosDeEjemplo
{
    public static async Task CargarAsync(TurnosDbContext db, TimeProvider reloj, CancellationToken ct = default)
    {
        if (await db.Turnos.AnyAsync(ct)) return;

        var ahora = reloj.GetUtcNow().UtcDateTime;
        var manana = ahora.Date.AddDays(1);

        var turnos = new[]
        {
            Turno.Crear("Lucía Fernández", "600 111 222", "Corte", manana.AddHours(9), 30, null, ahora),
            Turno.Crear("Marcos Ibáñez", "600 333 444", "Barba", manana.AddHours(10), 20, "Prefiere máquina", ahora),
            Turno.Crear("Carla Soler", null, "Tintura", manana.AddHours(11), 90, "Alergia al amoníaco", ahora),
            Turno.Crear("Pablo Ruiz", "600 555 666", "Corte", manana.AddDays(1).AddHours(16), 30, null, ahora),
            Turno.Crear("Elena Navarro", "600 777 888", "Peinado", manana.AddDays(2).AddHours(12), 45, null, ahora),
            Turno.Crear("Sergio Gil", null, "Corte", manana.AddDays(2).AddHours(17), 30, null, ahora),
        };

        turnos[0].Confirmar();
        turnos[1].Confirmar();
        turnos[4].Cancelar();

        await db.Turnos.AddRangeAsync(turnos, ct);
        await db.SaveChangesAsync(ct);
    }
}
