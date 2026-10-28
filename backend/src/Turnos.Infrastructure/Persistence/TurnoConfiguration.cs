using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Turnos.Domain.Entities;

namespace Turnos.Infrastructure.Persistence;

public class TurnoConfiguration : IEntityTypeConfiguration<Turno>
{
    public void Configure(EntityTypeBuilder<Turno> builder)
    {
        builder.ToTable("Turnos");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.ClienteNombre).IsRequired().HasMaxLength(100);
        builder.Property(t => t.ClienteTelefono).HasMaxLength(30);
        builder.Property(t => t.Servicio).IsRequired().HasMaxLength(100);
        builder.Property(t => t.Notas).HasMaxLength(500);
        builder.Property(t => t.Estado).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(t => t.Inicio);
        builder.HasIndex(t => new { t.Estado, t.Inicio });
    }
}
