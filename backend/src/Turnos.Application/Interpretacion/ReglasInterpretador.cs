using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Turnos.Application.Abstractions;
using Turnos.Application.Common;
using Turnos.Application.Dtos;

namespace Turnos.Application.Interpretacion;

/// <summary>
/// Intérprete determinista basado en expresiones regulares. No necesita red ni claves:
/// entiende lo básico ("mañana a las 15", "el jueves a las 4 de la tarde", "12/11 a las 10:30")
/// y sirve de respaldo cuando el modelo de IA no está disponible.
/// </summary>
public sealed partial class ReglasInterpretador(ZonaHorariaLocal zona) : IInterpretadorMensajes
{
    public const int DuracionPorDefectoMinutos = 30;

    private static readonly string[] Dias =
        ["domingo", "lunes", "martes", "miercoles", "jueves", "viernes", "sabado"];

    private static readonly string[] Meses =
        ["enero", "febrero", "marzo", "abril", "mayo", "junio",
         "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

    private static readonly (string Palabra, string Servicio)[] Servicios =
    [
        ("corte", "Corte"), ("tinte", "Tintura"), ("tintura", "Tintura"), ("mechas", "Mechas"),
        ("barba", "Barba"), ("peinado", "Peinado"), ("manicura", "Manicura"), ("pedicura", "Pedicura"),
        ("masaje", "Masaje"), ("limpieza", "Limpieza"), ("consulta", "Consulta"), ("revision", "Revisión")
    ];

    public Task<TurnoSugeridoDto> InterpretarAsync(string mensaje, DateTime ahoraUtc, CancellationToken ct)
    {
        var normalizado = Normalizar(mensaje);
        var ahoraLocal = zona.ALocal(ahoraUtc);

        var hora = DetectarHora(normalizado);
        var fecha = DetectarFecha(normalizado, ahoraLocal);

        DateTime? inicioUtc = fecha is not null && hora is not null
            ? zona.AUtc(fecha.Value.Add(hora.Value))
            : null;

        var sugerencia = new TurnoSugeridoDto(
            ClienteNombre: DetectarNombre(mensaje),
            Servicio: DetectarServicio(normalizado),
            Inicio: inicioUtc,
            DuracionMinutos: DuracionPorDefectoMinutos,
            Notas: null,
            Origen: "reglas");

        return Task.FromResult(sugerencia);
    }

    /// <summary>Minúsculas, sin tildes y con los momentos del día convertidos en marcas (am/pm).</summary>
    internal static string Normalizar(string texto)
    {
        var descompuesto = texto.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var limpio = sb.ToString().Normalize(NormalizationForm.FormC);
        limpio = MomentoManana().Replace(limpio, " am ");
        limpio = MomentoTardeNoche().Replace(limpio, " pm ");
        return limpio;
    }

    private static TimeSpan? DetectarHora(string texto)
    {
        var m = HoraRegex().Match(texto);
        if (!m.Success) return null;

        var hora = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var minuto = m.Groups[2].Success ? int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
        if (hora > 23 || minuto > 59) return null;

        var esPm = texto.Contains(" pm ", StringComparison.Ordinal);
        var esAm = texto.Contains(" am ", StringComparison.Ordinal);

        if (esPm && hora < 12) hora += 12;
        else if (esAm && hora == 12) hora = 0;
        else if (!esPm && !esAm && hora is >= 1 and <= 7) hora += 12; // nadie reserva a las 5 de la madrugada

        return new TimeSpan(hora, minuto, 0);
    }

    private static DateTime? DetectarFecha(string texto, DateTime ahoraLocal)
    {
        var hoy = ahoraLocal.Date;

        var conMes = FechaConMesRegex().Match(texto);
        if (conMes.Success)
        {
            var mes = Array.IndexOf(Meses, conMes.Groups[2].Value) + 1;
            return ConstruirFecha(int.Parse(conMes.Groups[1].Value, CultureInfo.InvariantCulture), mes, null, hoy);
        }

        var numerica = FechaNumericaRegex().Match(texto);
        if (numerica.Success)
        {
            int? anio = numerica.Groups[3].Success
                ? NormalizarAnio(int.Parse(numerica.Groups[3].Value, CultureInfo.InvariantCulture))
                : null;
            return ConstruirFecha(
                int.Parse(numerica.Groups[1].Value, CultureInfo.InvariantCulture),
                int.Parse(numerica.Groups[2].Value, CultureInfo.InvariantCulture),
                anio, hoy);
        }

        if (PasadoMananaRegex().IsMatch(texto)) return hoy.AddDays(2);
        if (MananaRegex().IsMatch(texto)) return hoy.AddDays(1);
        if (HoyRegex().IsMatch(texto)) return hoy;

        for (var i = 0; i < Dias.Length; i++)
        {
            if (!Regex.IsMatch(texto, $@"\b{Dias[i]}\b")) continue;

            var diferencia = ((i - (int)hoy.DayOfWeek) + 7) % 7;
            return hoy.AddDays(diferencia == 0 ? 7 : diferencia);
        }

        return null;
    }

    private static DateTime? ConstruirFecha(int dia, int mes, int? anio, DateTime hoy)
    {
        try
        {
            var fecha = new DateTime(anio ?? hoy.Year, mes, dia);
            if (anio is null && fecha < hoy) fecha = fecha.AddYears(1);
            return fecha;
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static int NormalizarAnio(int anio) => anio < 100 ? 2000 + anio : anio;

    private static string? DetectarNombre(string mensajeOriginal)
    {
        var m = NombreRegex().Match(mensajeOriginal);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static string? DetectarServicio(string texto)
    {
        foreach (var (palabra, servicio) in Servicios)
        {
            if (Regex.IsMatch(texto, $@"\b{palabra}\b"))
                return servicio;
        }

        return null;
    }

    [GeneratedRegex(@"\b(?:de|por|en) la manana\b")]
    private static partial Regex MomentoManana();

    [GeneratedRegex(@"\b(?:de|por|en) la (?:tarde|noche)\b")]
    private static partial Regex MomentoTardeNoche();

    [GeneratedRegex(@"\b(?:a\s+las?|las?)\s*(\d{1,2})(?:\s*[:.h]\s*(\d{2}))?")]
    private static partial Regex HoraRegex();

    [GeneratedRegex(@"\b(\d{1,2})\s+de\s+(enero|febrero|marzo|abril|mayo|junio|julio|agosto|septiembre|octubre|noviembre|diciembre)\b")]
    private static partial Regex FechaConMesRegex();

    [GeneratedRegex(@"\b(\d{1,2})[/-](\d{1,2})(?:[/-](\d{2,4}))?\b")]
    private static partial Regex FechaNumericaRegex();

    [GeneratedRegex(@"\bpasado manana\b")]
    private static partial Regex PasadoMananaRegex();

    [GeneratedRegex(@"\bmanana\b")]
    private static partial Regex MananaRegex();

    [GeneratedRegex(@"\bhoy\b")]
    private static partial Regex HoyRegex();

    [GeneratedRegex(@"\b(?i:soy|me llamo|mi nombre es)\s+(\p{Lu}\p{L}+(?:\s+\p{Lu}\p{L}+)?)")]
    private static partial Regex NombreRegex();
}
