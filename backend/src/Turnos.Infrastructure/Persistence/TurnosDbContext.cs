using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Turnos.Domain.Entities;

namespace Turnos.Infrastructure.Persistence;

public class TurnosDbContext(DbContextOptions<TurnosDbContext> options) : DbContext(options)
{
    public DbSet<Turno> Turnos => Set<Turno>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TurnosDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
}

/// <summary>
/// SQLite devuelve las fechas sin zona (Kind = Unspecified). El dominio trabaja siempre en UTC,
/// así que se marca explícitamente para que al serializar a JSON salga con la "Z".
/// </summary>
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
