using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; // <-- necesario para [ValidateNever]

namespace Sistema_de_Ticket.Models
{
    public enum EstadoTicket { Abierto = 0, EnProceso = 1, Resuelto = 2, Cerrado = 3 }
    public enum SeveridadTicket { Baja = 0, Media = 1, Alta = 2, Critica = 3 }

    public class Ticket
    {
        public int Id { get; set; }

        [Required, StringLength(120)]
        public string Titulo { get; set; } = string.Empty;

        [Required, StringLength(2000)]
        public string Descripcion { get; set; } = string.Empty;

        [Required]
        public EstadoTicket Estado { get; set; } = EstadoTicket.Abierto;

        [Required]
        public SeveridadTicket Severidad { get; set; } = SeveridadTicket.Media;

        // FK obligatoria: la completa el controlador en el POST
        [Required]
        public string UsuarioCreadorId { get; set; } = string.Empty;

        // Navegación: NO validar (evita “The UsuarioCreador field is required”)
        [ValidateNever]
        public AppUser UsuarioCreador { get; set; } = null!;

        public string? TecnicoAsignadoId { get; set; }

        // Navegación: NO validar
        [ValidateNever]
        public AppUser? TecnicoAsignado { get; set; }

        [Required]
        public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

        public DateTime? FechaCierre { get; set; }

        [StringLength(4000)]
        public string? Notas { get; set; }
    }
}
