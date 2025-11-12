using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Sistema_de_Ticket.Models
{
    public class AppUser : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string NombreUsuario { get; set; } = string.Empty;
    }
}


