using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SanPatricioRugby.DAL.Models;

namespace SanPatricioRugby.DAL
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Socio> Socios { get; set; }
        public DbSet<Cuota> Cuotas { get; set; }
        public DbSet<RegistroIngreso> Ingresos { get; set; }
        public DbSet<RegistroEstacionamiento> Estacionamientos { get; set; }
        public DbSet<ConfiguracionPrecio> Precios { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            
            // Decimal precision for amounts
            builder.Entity<Cuota>().Property(c => c.Monto).HasColumnType("decimal(18,2)");
            builder.Entity<RegistroIngreso>().Property(r => r.MontoPagado).HasColumnType("decimal(18,2)");
            builder.Entity<RegistroEstacionamiento>().Property(r => r.MontoPagado).HasColumnType("decimal(18,2)");
            builder.Entity<ConfiguracionPrecio>().Property(c => c.Valor).HasColumnType("decimal(18,2)");
        }
    }
}
