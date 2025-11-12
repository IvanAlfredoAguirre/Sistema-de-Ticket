using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sistema_de_Ticket.Models;
using Sistema_de_Ticket.ViewModels;

namespace Sistema_de_Ticket.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class UsersController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: /Users
        public async Task<IActionResult> Index()
        {
            var users = _userManager.Users.ToList();
            var model = new List<dynamic>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                model.Add(new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    user.NombreUsuario,
                    Role = string.Join(", ", roles)
                });
            }

            return View(model);
        }

        // GET: /Users/Create
        public IActionResult Create()
        {
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();
            return View(new CreateUserVM());
        }

        // POST: /Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserVM model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();
                return View(model);
            }

            // Unicidad
            if (await _userManager.FindByNameAsync(model.UserName) != null)
                ModelState.AddModelError(nameof(model.UserName), "Ya existe un usuario con ese nombre.");

            if (!string.IsNullOrWhiteSpace(model.Email) &&
                await _userManager.FindByEmailAsync(model.Email) != null)
                ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con ese email.");

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();
                return View(model);
            }

            var user = new AppUser
            {
                UserName = model.UserName,
                NombreUsuario = string.IsNullOrWhiteSpace(model.NombreUsuario) ? model.UserName : model.NombreUsuario,
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);

                ViewBag.Roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();
                return View(model);
            }

            // Rol
            if (!string.IsNullOrWhiteSpace(model.RoleName))
            {
                if (!await _roleManager.RoleExistsAsync(model.RoleName))
                    await _roleManager.CreateAsync(new IdentityRole(model.RoleName));

                await _userManager.AddToRoleAsync(user, model.RoleName);
            }

            TempData["ok"] = $"Usuario {model.UserName} creado.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Users/Edit/ID
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var rolesUsuario = await _userManager.GetRolesAsync(user);
            var rolesDisponibles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();

            var vm = new EditUserVM
            {
                Id = user.Id,
                UserName = user.UserName!,
                NombreUsuario = user.NombreUsuario,
                Email = user.Email,
                SelectedRole = rolesUsuario.FirstOrDefault(),
                Roles = rolesDisponibles
            };

            return View(vm);
        }

        // POST: /Users/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserVM vm)
        {
            var user = await _userManager.FindByIdAsync(vm.Id);
            if (user == null) return NotFound();

            // Unicidad UserName
            var byName = await _userManager.FindByNameAsync(vm.UserName);
            if (byName != null && byName.Id != user.Id)
                ModelState.AddModelError(nameof(vm.UserName), "Ya existe un usuario con ese nombre.");

            // Unicidad Email
            if (!string.IsNullOrWhiteSpace(vm.Email))
            {
                var byEmail = await _userManager.FindByEmailAsync(vm.Email);
                if (byEmail != null && byEmail.Id != user.Id)
                    ModelState.AddModelError(nameof(vm.Email), "Ya existe un usuario con ese email.");
            }

            // Rol válido si viene
            if (!string.IsNullOrWhiteSpace(vm.SelectedRole) &&
                !_roleManager.Roles.Any(r => r.Name == vm.SelectedRole))
            {
                ModelState.AddModelError(nameof(vm.SelectedRole), "El rol seleccionado no existe.");
            }

            if (!ModelState.IsValid)
            {
                vm.Roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();
                return View(vm);
            }

            // Datos básicos
            user.UserName = vm.UserName;
            user.NombreUsuario = string.IsNullOrWhiteSpace(vm.NombreUsuario) ? vm.UserName : vm.NombreUsuario;
            user.Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email;

            var upd = await _userManager.UpdateAsync(user);
            if (!upd.Succeeded)
            {
                foreach (var e in upd.Errors) ModelState.AddModelError("", e.Description);
                vm.Roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();
                return View(vm);
            }

            // Cambio de rol (opcional)
            if (!string.IsNullOrWhiteSpace(vm.SelectedRole))
            {
                if (!await _roleManager.RoleExistsAsync(vm.SelectedRole))
                    await _roleManager.CreateAsync(new IdentityRole(vm.SelectedRole));

                var rolesActuales = await _userManager.GetRolesAsync(user);
                if (rolesActuales.Any())
                    await _userManager.RemoveFromRolesAsync(user, rolesActuales);

                await _userManager.AddToRoleAsync(user, vm.SelectedRole);
            }

            // Cambio de contraseña (opcional)
            if (!string.IsNullOrWhiteSpace(vm.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passRes = await _userManager.ResetPasswordAsync(user, token, vm.NewPassword);
                if (!passRes.Succeeded)
                {
                    foreach (var e in passRes.Errors) ModelState.AddModelError("", e.Description);
                    vm.Roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();
                    return View(vm);
                }
            }

            TempData["ok"] = "Usuario actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // Acciones para edición de rol separada (opcional). Puedes eliminarlas si no las usas.
        public async Task<IActionResult> EditRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var userRoles = await _userManager.GetRolesAsync(user);
            var roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();

            ViewBag.Roles = roles;
            ViewBag.CurrentRoles = userRoles;

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(string id, string newRole)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (!await _roleManager.RoleExistsAsync(newRole))
                await _roleManager.CreateAsync(new IdentityRole(newRole));

            var old = await _userManager.GetRolesAsync(user);
            if (old.Any())
                await _userManager.RemoveFromRolesAsync(user, old);

            await _userManager.AddToRoleAsync(user, newRole);

            TempData["ok"] = $"Rol de {user.UserName} actualizado a {newRole}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Users/Delete/ID
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: /Users/Delete/ID
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var res = await _userManager.DeleteAsync(user);
            TempData[res.Succeeded ? "ok" : "err"] =
                res.Succeeded ? "Usuario eliminado" : string.Join(",", res.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Index));
        }
    }
}
