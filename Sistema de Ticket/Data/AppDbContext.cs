using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Ticket.Models;

namespace Sistema_de_Ticket.Data
{
    // DbContext: puente entre las clases (modelo) y la base de datos.
    public class AppDbContext : IdentityDbContext<AppUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Tablas propias del sistema
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Notificacion> Notificaciones { get; set; }

    }
}
