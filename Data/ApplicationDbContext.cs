using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public DbSet<Solicitud> Solicitudes => Set<Solicitud>();

    public DbSet<Cliente> Clientes => Set<Cliente>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Solicitud>(entity =>
        {
            entity.ToTable("Solicitudes");

            // SQLite no soporta decimal nativo: se almacena como REAL (double)
            // para que las comparaciones de rango sean correctas.
            entity.Property(s => s.MontoSolicitado).HasConversion<double>();

            entity.HasIndex(s => s.UserId);
            entity.HasIndex(s => s.Estado);
        });

        modelBuilder.Entity<Cliente>(entity =>
        {
            entity.ToTable("Clientes");

            entity.Property(c => c.IngresosMensuales).HasConversion<double>();

            // Un usuario = un único cliente.
            entity.HasIndex(c => c.UserId).IsUnique();
        });
    }
}