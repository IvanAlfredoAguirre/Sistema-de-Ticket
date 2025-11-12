using System.ComponentModel.DataAnnotations;

namespace Sistema_de_Ticket.Models
{
    public class Notificacion
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket? Ticket { get; set; }

        public DateTime FechaHora { get; set; } = DateTime.UtcNow;

        [Required, StringLength(100)]
        public string Tipo { get; set; } = "TicketEvento";

        [Required, StringLength(500)]
        public string Mensaje { get; set; } = "";

        public string? UsuarioId { get; set; }
        public AppUser? Usuario { get; set; }
    }
}
