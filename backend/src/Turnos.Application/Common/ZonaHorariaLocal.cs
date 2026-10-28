namespace Turnos.Application.Common;

/// <summary>
/// Zona horaria del negocio. El sistema guarda todo en UTC y solo convierte
/// a hora local para interpretar mensajes como "mañana a las 15".
/// </summary>
public sealed class ZonaHorariaLocal(TimeZoneInfo zona)
{
    public const string IdPorDefecto = "Europe/Madrid";

    public TimeZoneInfo Zona { get; } = zona;

    public static ZonaHorariaLocal Desde(string? id)
    {
        try
        {
            var buscada = string.IsNullOrWhiteSpace(id) ? IdPorDefecto : id;
            return new ZonaHorariaLocal(TimeZoneInfo.FindSystemTimeZoneById(buscada));
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return new ZonaHorariaLocal(TimeZoneInfo.Utc);
        }
    }

    public DateTime ALocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona);

    public DateTime AUtc(DateTime local)
    {
        var sinKind = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (Zona.IsInvalidTime(sinKind))
            sinKind = sinKind.AddHours(1); // hora inexistente por cambio de horario
        return TimeZoneInfo.ConvertTimeToUtc(sinKind, Zona);
    }
}
