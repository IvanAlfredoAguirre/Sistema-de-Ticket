using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sistema_de_Ticket.Models;
using System.Threading.Tasks;

namespace Sistema_de_Ticket.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<AppUser> _signInManager;

        public AccountController(SignInManager<AppUser> signInManager)
        {
            _signInManager = signInManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Cierra la sesión del usuario actual
            await _signInManager.SignOutAsync();

            // Redirige al login de Identity
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }
    }
}
