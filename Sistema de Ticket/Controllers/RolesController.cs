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
    [Authorize]   // 👈 SOLO requiere estar logueado
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;

        public RolesController(RoleManager<IdentityRole> roleManager)
        {
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
        // LISTADO DE ROLES
        // ============================================================
        public IActionResult Index()
        {
            if (!TienePermiso(Permisos.RolesVer))
                return Forbid();

            var roles = _roleManager.Roles
                .Select(r => new RoleListItemVM
                {
                    Id = r.Id,
                    Name = r.Name!
                })
                .ToList();

            return View(roles);
        }

        // ============================================================
        // CREAR (GET)
        // ============================================================
        public IActionResult Create()
        {
            if (!TienePermiso(Permisos.RolesCrear))
                return Forbid();

            var vm = new RoleEditVM
            {
                Permisos = Permisos.Todos
                    .Select(p => new PermisoItemVM
                    {
                        Codigo = p,
                        Titulo = FormatearPermiso(p),
                        Seleccionado = false
                    })
                    .ToList()
            };

            return View(vm);
        }

        // ============================================================
        // CREAR (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoleEditVM vm)
        {
            if (!TienePermiso(Permisos.RolesCrear))
                return Forbid();

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

            var seleccionados = vm.Permisos
                .Where(x => x.Seleccionado)
                .Select(x => x.Codigo);

            foreach (var p in seleccionados)
                await _roleManager.AddClaimAsync(role, new Claim(AuthorizationConfig.PermissionClaimType, p));

            TempData["ok"] = "Rol creado correctamente.";
            return RedirectToAction(nameof(Index));
        }


        // ============================================================
        // EDITAR (GET)
        // ============================================================
        public async Task<IActionResult> Edit(string id)
        {
            if (!TienePermiso(Permisos.RolesEditar))
                return Forbid();

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

        // ============================================================
        // EDITAR (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoleEditVM vm)
        {
            if (!TienePermiso(Permisos.RolesEditar))
                return Forbid();

            if (vm.Id is null)
                return BadRequest();

            var role = await _roleManager.FindByIdAsync(vm.Id);
            if (role == null) return NotFound();

            vm.Permisos ??= new List<PermisoItemVM>();

            if (!ModelState.IsValid)
            {
                var actuales = (await _roleManager.GetClaimsAsync(role))
                    .Where(c => c.Type == AuthorizationConfig.PermissionClaimType)
                    .Select(c => c.Value)
                    .ToHashSet();

                vm.Permisos = Permisos.Todos
                    .Select(p => new PermisoItemVM
                    {
                        Codigo = p,
                        Titulo = FormatearPermiso(p),
                        Seleccionado = actuales.Contains(p)
                    })
                    .ToList();

                return View(vm);
            }

            // Actualizar nombre
            role.Name = vm.Name;
            var upd = await _roleManager.UpdateAsync(role);

            if (!upd.Succeeded)
            {
                foreach (var e in upd.Errors)
                    ModelState.AddModelError("", e.Description);

                return View(vm);
            }

            // Claims actuales
            var claimsExistentes = await _roleManager.GetClaimsAsync(role);
            var claimsPermisos = claimsExistentes
                .Where(c => c.Type == AuthorizationConfig.PermissionClaimType)
                .ToList();

            // 1. Eliminar permisos desmarcados
            foreach (var c in claimsPermisos)
            {
                bool sigue = vm.Permisos.Any(p => p.Seleccionado && p.Codigo == c.Value);

                if (!sigue)
                    await _roleManager.RemoveClaimAsync(role, c);
            }

            // 2. Agregar permisos nuevos
            var actualesValores = claimsPermisos.Select(c => c.Value).ToHashSet();

            foreach (var p in vm.Permisos.Where(x => x.Seleccionado).Select(x => x.Codigo))
            {
                if (!actualesValores.Contains(p))
                    await _roleManager.AddClaimAsync(role, new Claim(AuthorizationConfig.PermissionClaimType, p));
            }

            TempData["ok"] = "Rol actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // ELIMINAR (POST)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (!TienePermiso(Permisos.RolesEliminar))
                return Forbid();

            var role = await _roleManager.FindByIdAsync(id);
            if (role == null) return NotFound();

            var res = await _roleManager.DeleteAsync(role);

            TempData[res.Succeeded ? "ok" : "err"] =
                res.Succeeded
                    ? "Rol eliminado."
                    : string.Join(", ", res.Errors.Select(e => e.Description));

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // FORMATEAR PERMISO
        // ============================================================
        private string FormatearPermiso(string codigo)
        {
            return codigo.Replace('.', ' ').ToUpper();
        }
    }
}
