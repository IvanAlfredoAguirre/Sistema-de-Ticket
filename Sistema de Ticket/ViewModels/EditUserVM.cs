using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sistema_de_Ticket.ViewModels
{
    public class EditUserVM
    {
        [Required]
        public string Id { get; set; } = "";

        [Required(ErrorMessage = "El nombre de usuario es obligatorio.")]
        [Display(Name = "Usuario")]
        public string UserName { get; set; } = "";

        [Display(Name = "Nombre visible")]
        public string? NombreUsuario { get; set; }

        [EmailAddress]
        [Display(Name = "Email (opcional)")]
        public string? Email { get; set; }

        [Display(Name = "Rol")]
        public string? SelectedRole { get; set; }

        public List<string> Roles { get; set; } = new();

        // Cambio de contraseña (opcional)
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        [Display(Name = "Nueva contraseña (opcional)")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "La confirmación de contraseña no coincide.")]
        [Display(Name = "Confirmar nueva contraseña")]
        public string? ConfirmNewPassword { get; set; }
    }
}
