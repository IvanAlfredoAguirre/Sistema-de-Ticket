using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sistema_de_Ticket.Auth;
using Sistema_de_Ticket.ViewModels;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Sistema_de_Ticket.Controllers
{
    [Authorize(Roles = "SuperAdmin")]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        // GET: /Roles
        public IActionResult Index()
        {
            var roles = _roleManager.Roles
                .Select(r => new RoleListItemVM { Id = r.Id, Name = r.Name! })
                .ToList();

            return View(roles);
        }

        // GET: /Roles/Create
        public IActionResult Create()
        {
            var vm = new RoleEditVM
            {
                Permisos = Permisos.Todos
                    .Select(p => new PermisoItemVM { Codigo = p, Titulo = p })
                    .ToList()
            };
            return View(vm);
        }

        // POST: /Roles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleEditVM vm)
        {
            if (vm.Permisos == null)
                vm.Permisos = new List<PermisoItemVM>();

            if (!ModelState.IsValid)
            {
                if (vm.Permisos.Count == 0)
                    vm.Permisos = Permisos.Todos.Select(p => new PermisoItemVM { Codigo = p, Titulo = p }).ToList();

                return View(vm);
            }

            var role = new IdentityRole(vm.Name);
            var res = await _roleManager.CreateAsync(role);
            if (!res.Succeeded)
            {
                foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
                if (vm.Permisos.Count == 0)
                    vm.Permisos = Permisos.Todos.Select(p => new PermisoItemVM { Codigo = p, Titulo = p }).ToList();
                return View(vm);
            }

            var seleccionados = vm.Permisos.Where(x => x.Seleccionado).Select(x => x.Codigo);
            foreach (var p in seleccionados)
                await _roleManager.AddClaimAsync(role, new Claim(AuthorizationConfig.PermissionClaimType, p));

            TempData["ok"] = "Rol creado.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Roles/Edit/ID
        public async Task<IActionResult> Edit(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            var claims = await _roleManager.GetClaimsAsync(role);
            var permisosActuales = claims
                .Where(c => c.Type == AuthorizationConfig.PermissionClaimType)
                .Select(c => c.Value)
                .ToHashSet();

            var vm = new RoleEditVM
            {
                Id = role.Id,
                Name = role.Name ?? "",
                Permisos = Permisos.Todos
                    .Select(p => new PermisoItemVM
                    {
                        Codigo = p,
                        Titulo = p,
                        Seleccionado = permisosActuales.Contains(p)
                    })
                    .ToList()
            };

            return View(vm);
        }

        // POST: /Roles/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoleEditVM vm)
        {
            if (string.IsNullOrWhiteSpace(vm.Id))
                return BadRequest();

            var role = await _roleManager.FindByIdAsync(vm.Id);
            if (role == null) return NotFound();

            if (vm.Permisos == null)
                vm.Permisos = new List<PermisoItemVM>();

            if (!ModelState.IsValid)
            {
                if (vm.Permisos.Count == 0)
                {
                    var claims = await _roleManager.GetClaimsAsync(role);
                    var permisosActuales = claims
                        .Where(c => c.Type == AuthorizationConfig.PermissionClaimType)
                        .Select(c => c.Value)
                        .ToHashSet();

                    vm.Permisos = Permisos.Todos
                        .Select(p => new PermisoItemVM
                        {
                            Codigo = p,
                            Titulo = p,
                            Seleccionado = permisosActuales.Contains(p)
                        })
                        .ToList();
                }
                return View(vm);
            }

            role.Name = vm.Name;
            var upd = await _roleManager.UpdateAsync(role);
            if (!upd.Succeeded)
            {
                foreach (var e in upd.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            var claimsExistentes = await _roleManager.GetClaimsAsync(role);
            var claimsPermisos = claimsExistentes
                .Where(c => c.Type == AuthorizationConfig.PermissionClaimType)
                .ToList();

            foreach (var c in claimsPermisos)
            {
                var sigue = vm.Permisos.Any(p => p.Seleccionado && p.Codigo == c.Value);
                if (!sigue)
                    await _roleManager.RemoveClaimAsync(role, c);
            }

            var valoresActuales = claimsPermisos.Select(c => c.Value).ToHashSet();
            foreach (var p in vm.Permisos.Where(x => x.Seleccionado).Select(x => x.Codigo))
            {
                if (!valoresActuales.Contains(p))
                    await _roleManager.AddClaimAsync(role, new Claim(AuthorizationConfig.PermissionClaimType, p));
            }

            TempData["ok"] = "Rol actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Roles/Delete/ID
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            var res = await _roleManager.DeleteAsync(role);
            TempData[res.Succeeded ? "ok" : "err"] =
                res.Succeeded ? "Rol eliminado." : string.Join(", ", res.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Index));
        }
    }
}
