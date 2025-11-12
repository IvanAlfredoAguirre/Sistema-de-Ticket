using System.ComponentModel.DataAnnotations;

namespace Sistema_de_Ticket.ViewModels
{
    public class CreateUserVM
    {
        [Required, Display(Name = "Usuario")]
        public string UserName { get; set; } = string.Empty;

        [Display(Name = "Nombre visible")]
        public string? NombreUsuario { get; set; }

        [EmailAddress, Display(Name = "Email (opcional)")]
        public string? Email { get; set; }

        [Required, DataType(DataType.Password), Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Display(Name = "Confirmar contraseña")]
        [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required, Display(Name = "Rol")]
        public string RoleName { get; set; } = "Usuario";
    }
}
