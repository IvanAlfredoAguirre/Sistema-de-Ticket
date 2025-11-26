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
    // SOLO SuperAdmin puede administrar Roles
    [Authorize(Roles = "SuperAdmin")]
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RolesController(RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
        }

        // ======================================================================================
        // LISTADO
        // ======================================================================================
        public IActionResult Index()
        {
            var roles = _roleManager.Roles
                .Select(r => new RoleListItemVM { Id = r.Id, Name = r.Name! })
                .ToList();

            return View(roles);
        }

        // ======================================================================================
        // CREAR (GET)
        // ======================================================================================
        public IActionResult Create()
        {
            var vm = new RoleEditVM
            {
                Permisos = Permisos.Todos
                    .Select(p => new PermisoItemVM
                    {
                        Codigo = p,
                        Titulo = FormatearPermiso(p),
                        Seleccionado = false
                    }).ToList()
            };
            return View(vm);
        }

        // ======================================================================================
        // CREAR (POST)
        // ======================================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleEditVM vm)
        {
            vm.Permisos ??= new List<PermisoItemVM>();

            if (!ModelState.IsValid)
            {
                vm.Permisos = Permisos.Todos
                    .Select(p => new PermisoItemVM { Codigo = p, Titulo = FormatearPermiso(p) })
                    .ToList();

                return View(vm);
            }

            var role = new IdentityRole(vm.Name);
            var res = await _roleManager.CreateAsync(role);

            if (!res.Succeeded)
            {
                foreach (var e in res.Errors)
                    ModelState.AddModelError("", e.Description);

                vm.Permisos = Permisos.Todos
                    .Select(p => new PermisoItemVM { Codigo = p, Titulo = FormatearPermiso(p) })
                    .ToList();

                return View(vm);
            }

            var seleccionados = vm.Permisos.Where(x => x.Seleccionado).Select(x => x.Codigo);

            foreach (var p in seleccionados)
                await _roleManager.AddClaimAsync(role, new Claim(AuthorizationConfig.PermissionClaimType, p));

            TempData["ok"] = "Rol creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ======================================================================================
        // EDITAR (GET)
        // ======================================================================================
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
                Name = role.Name!,
                Permisos = Permisos.Todos
                    .Select(p => new PermisoItemVM
                    {
                        Codigo = p,
                        Titulo = FormatearPermiso(p),
                        Seleccionado = permisosActuales.Contains(p)
                    })
                    .ToList()
            };

            return View(vm);
        }

        // ======================================================================================
        // EDITAR (POST)
        // ======================================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoleEditVM vm)
        {
            if (vm.Id is null)
                return BadRequest();

            var role = await _roleManager.FindByIdAsync(vm.Id);
            if (role == null) return NotFound();

            vm.Permisos ??= new List<PermisoItemVM>();

            if (!ModelState.IsValid)
            {
                var claimsActuales = await _roleManager.GetClaimsAsync(role);
                var actuales = claimsActuales
                    .Where(c => c.Type == AuthorizationConfig.PermissionClaimType)
                    .Select(c => c.Value)
                    .ToHashSet();

                vm.Permisos = Permisos.Todos
                    .Select(p => new PermisoItemVM
                    {
                        Codigo = p,
                        Titulo = FormatearPermiso(p),
                        Seleccionado = actuales.Contains(p)
                    }).ToList();

                return View(vm);
            }

            role.Name = vm.Name;
            var upd = await _roleManager.UpdateAsync(role);

            if (!upd.Succeeded)
            {
                foreach (var e in upd.Errors) ModelState.AddModelError("", e.Description);
                return View(vm);
            }

            // Claims actuales del rol
            var claimsExistentes = await _roleManager.GetClaimsAsync(role);
            var claimsPermisos = claimsExistentes
                .Where(c => c.Type == AuthorizationConfig.PermissionClaimType)
                .ToList();

            // 1. Eliminar permisos que ya NO están marcados
            foreach (var c in claimsPermisos)
            {
                var sigue = vm.Permisos.Any(p => p.Seleccionado && p.Codigo == c.Value);
                if (!sigue)
                    await _roleManager.RemoveClaimAsync(role, c);
            }

            // 2. Agregar NUEVOS permisos seleccionados
            var valoresActuales = claimsPermisos.Select(c => c.Value).ToHashSet();

            foreach (var p in vm.Permisos.Where(x => x.Seleccionado).Select(x => x.Codigo))
            {
                if (!valoresActuales.Contains(p))
                    await _roleManager.AddClaimAsync(role, new Claim(AuthorizationConfig.PermissionClaimType, p));
            }

            TempData["ok"] = "Rol actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ======================================================================================
        // ELIMINAR
        // ======================================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            var res = await _roleManager.DeleteAsync(role);

            TempData[res.Succeeded ? "ok" : "err"] =
                res.Succeeded ? "Rol eliminado." :
                string.Join(", ", res.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Index));
        }

        // ======================================================================================
        // FORMATEADOR → Mejora visual de permisos
        // ======================================================================================
        private string FormatearPermiso(string codigo)
        {
            return codigo.Replace('.', ' ').ToUpper();
        }
    }
}
