using Microsoft.EntityFrameworkCore;
using SalonEventos.Models;

namespace SalonEventos.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Evento> Eventos { get; set; }
        public DbSet<Movimiento> Movimientos { get; set; }
        public DbSet<ReporteMensual> ReportesMensuales { get; set; }
    }
}
