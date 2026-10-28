using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Turnos.Application.Abstractions;
using Turnos.Application.Common;
using Turnos.Application.Interpretacion;
using Turnos.Infrastructure.Interpretacion;
using Turnos.Infrastructure.Persistence;

namespace Turnos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<TurnosDbContext>((sp, o) => o.UseSqlite(
            sp.GetRequiredService<IConfiguration>().GetConnectionString("Default") ?? "Data Source=turnos.db"));
        services.AddScoped<ITurnoRepository, TurnoRepository>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(ZonaHorariaLocal.Desde(config["Turnos:ZonaHoraria"]));

        services.Configure<GeminiOptions>(config.GetSection(GeminiOptions.Seccion));
        services.AddSingleton<ReglasInterpretador>();

        var gemini = config.GetSection(GeminiOptions.Seccion).Get<GeminiOptions>() ?? new GeminiOptions();
        if (gemini.Habilitado)
        {
            services.AddHttpClient<GeminiInterpretador>(http =>
            {
                http.BaseAddress = new Uri(gemini.BaseUrl);
                http.Timeout = TimeSpan.FromSeconds(gemini.TimeoutSegundos);
            });
            services.AddScoped<IInterpretadorMensajes>(sp => new InterpretadorConRespaldo(
                sp.GetRequiredService<GeminiInterpretador>(),
                sp.GetRequiredService<ReglasInterpretador>(),
                sp.GetRequiredService<ILogger<InterpretadorConRespaldo>>()));
        }
        else
        {
            services.AddScoped<IInterpretadorMensajes>(sp => sp.GetRequiredService<ReglasInterpretador>());
        }

        return services;
    }
}
