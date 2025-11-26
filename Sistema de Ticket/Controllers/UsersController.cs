using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sistema_de_Ticket.Models;
using Sistema_de_Ticket.ViewModels;
using Sistema_de_Ticket.Auth;   // 👈 IMPORTANTE

namespace Sistema_de_Ticket.Controllers
{
    [Authorize]   // 👈 Ahora requiere login, no rol fijo
    public class UsersController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // ============================================================
        //  MÉTODO CENTRAL PARA VALIDAR PERMISOS
        // ============================================================
        private bool TienePermiso(string permiso)
        {
            if (User.IsInRole("SuperAdmin"))
                return true;

            return User.HasClaim(AuthorizationConfig.PermissionClaimType, permiso);
        }

        // ============================================================
        // LISTADO
        // ============================================================
        public async Task<IActionResult> Index()
        {
            if (!TienePermiso(Permisos.UsuariosVer))
                return Forbid();

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

        // ============================================================
        // CREAR (GET)
        // ============================================================
        public IActionResult Create()
        {
            if (!TienePermiso(Permisos.UsuariosCrear))
                return Forbid();

            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();
            return View(new CreateUserVM());
        }

        // ============================================================
        // CREAR (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserVM model)
        {
            if (!TienePermiso(Permisos.UsuariosCrear))
                return Forbid();

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();
                return View(model);
            }

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
                    ModelState.AddModelError("", e.Description);

                ViewBag.Roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.RoleName))
            {
                if (!await _roleManager.RoleExistsAsync(model.RoleName))
                    await _roleManager.CreateAsync(new IdentityRole(model.RoleName));

                await _userManager.AddToRoleAsync(user, model.RoleName);
            }

            TempData["ok"] = $"Usuario {model.UserName} creado.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // EDITAR (GET)
        // ============================================================
        public async Task<IActionResult> Edit(string id)
        {
            if (!TienePermiso(Permisos.UsuariosEditar))
                return Forbid();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var rolesUsuario = await _userManager.GetRolesAsync(user);
            var roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();

            var vm = new EditUserVM
            {
                Id = user.Id,
                UserName = user.UserName!,
                NombreUsuario = user.NombreUsuario,
                Email = user.Email,
                SelectedRole = rolesUsuario.FirstOrDefault(),
                Roles = roles
            };

            return View(vm);
        }

        // ============================================================
        // EDITAR (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserVM vm)
        {
            if (!TienePermiso(Permisos.UsuariosEditar))
                return Forbid();

            var user = await _userManager.FindByIdAsync(vm.Id);
            if (user == null) return NotFound();

            var byName = await _userManager.FindByNameAsync(vm.UserName);
            if (byName != null && byName.Id != user.Id)
                ModelState.AddModelError(nameof(vm.UserName), "Ya existe un usuario con ese nombre.");

            if (!string.IsNullOrWhiteSpace(vm.Email))
            {
                var byEmail = await _userManager.FindByEmailAsync(vm.Email);
                if (byEmail != null && byEmail.Id != user.Id)
                    ModelState.AddModelError(nameof(vm.Email), "Ya existe un usuario con ese email.");
            }

            if (!ModelState.IsValid)
            {
                vm.Roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();
                return View(vm);
            }

            user.UserName = vm.UserName;
            user.NombreUsuario = string.IsNullOrWhiteSpace(vm.NombreUsuario) ? vm.UserName : vm.NombreUsuario;
            user.Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email;

            var upd = await _userManager.UpdateAsync(user);
            if (!upd.Succeeded)
            {
                foreach (var e in upd.Errors)
                    ModelState.AddModelError("", e.Description);

                vm.Roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(vm.SelectedRole))
            {
                if (!await _roleManager.RoleExistsAsync(vm.SelectedRole))
                    await _roleManager.CreateAsync(new IdentityRole(vm.SelectedRole));

                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);

                await _userManager.AddToRoleAsync(user, vm.SelectedRole);
            }

            if (!string.IsNullOrWhiteSpace(vm.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passRes = await _userManager.ResetPasswordAsync(user, token, vm.NewPassword);

                if (!passRes.Succeeded)
                {
                    foreach (var e in passRes.Errors)
                        ModelState.AddModelError("", e.Description);

                    vm.Roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(n => n).ToList();
                    return View(vm);
                }
            }

            TempData["ok"] = "Usuario actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // ELIMINAR (GET)
        // ============================================================
        public async Task<IActionResult> Delete(string id)
        {
            if (!TienePermiso(Permisos.UsuariosEliminar))
                return Forbid();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            return View(user);
        }

        // ============================================================
        // ELIMINAR (POST)
        // ============================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            if (!TienePermiso(Permisos.UsuariosEliminar))
                return Forbid();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var res = await _userManager.DeleteAsync(user);

            TempData[res.Succeeded ? "ok" : "err"] =
                res.Succeeded ? "Usuario eliminado" : string.Join(", ", res.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Index));
        }
    }
}
