using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sistema_de_Ticket.ViewModels
{
    public class RoleEditVM
    {
        public string? Id { get; set; }

        [Required]
        [Display(Name = "Nombre del rol")]
        public string Name { get; set; } = "";

        public List<PermisoItemVM> Permisos { get; set; } = new();
    }
}
