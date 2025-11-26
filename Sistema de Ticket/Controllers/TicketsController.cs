using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Ticket.Data;
using Sistema_de_Ticket.Models;
using Sistema_de_Ticket.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sistema_de_Ticket.Auth;   // 👈 IMPORTANTE: para AuthorizationConfig

namespace Sistema_de_Ticket.Controllers
{
    [Authorize]
    public class TicketsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public TicketsController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // ============================================================
        //  MÉTODO CENTRAL PARA VALIDAR PERMISOS
        // ============================================================
        private bool TienePermiso(string permiso)
        {
            // SuperAdmin siempre tiene permiso
            if (User.IsInRole("SuperAdmin"))
                return true;

            // Usamos el mismo tipo que en AuthorizationConfig
            return User.HasClaim(AuthorizationConfig.PermissionClaimType, permiso);
            // AuthorizationConfig.PermissionClaimType = "permission"
        }

        // ============================================================
        //  INDEX (LISTADO)
        //  (NO PIDE PERMISO, solo requiere estar logueado)
        // ============================================================
        public async Task<IActionResult> Index(string? filtroEstado, string? filtroSeveridad)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);

            IQueryable<Ticket> qBase = _context.Tickets
                .Include(t => t.UsuarioCreador)
                .Include(t => t.TecnicoAsignado);

            // Filtros globales
            if (!string.IsNullOrWhiteSpace(filtroEstado) &&
                Enum.TryParse<EstadoTicket>(filtroEstado, true, out var est))
            {
                qBase = qBase.Where(t => t.Estado == est);
            }

            if (!string.IsNullOrWhiteSpace(filtroSeveridad) &&
                Enum.TryParse<SeveridadTicket>(filtroSeveridad, true, out var sev))
            {
                qBase = qBase.Where(t => t.Severidad == sev);
            }

            IQueryable<Ticket> qUser = qBase;

            // Si no es SuperAdmin, restringimos lo que ve
            if (!roles.Contains("SuperAdmin"))
            {
                if (roles.Contains("Técnico") || roles.Contains("Tecnico") || roles.Contains("Soporte"))
                {
                    // Técnicos → solo tickets asignados a ellos
                    qUser = qUser.Where(t => t.TecnicoAsignadoId == user.Id);
                }
                else
                {
                    // Usuarios normales → solo tickets creados por ellos
                    qUser = qUser.Where(t => t.UsuarioCreadorId == user.Id);
                }
            }

            // Métricas
            var enCurso = await qUser.CountAsync(t =>
                t.Estado == EstadoTicket.Abierto || t.Estado == EstadoTicket.EnProceso);

            var resueltos = await qUser.CountAsync(t => t.Estado == EstadoTicket.Resuelto);

            // Sin asignar → SIEMPRE global (cola de pendientes)
            var sinAsignar = await qBase.CountAsync(t => t.TecnicoAsignadoId == null);

            var asignados = await qUser
                .Where(t => t.TecnicoAsignadoId != null)
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

            var pendientes = await qBase
                .Where(t => t.TecnicoAsignadoId == null)
                .OrderByDescending(t => t.FechaCreacion)
                .ToListAsync();

            var vm = new TicketIndexVM
            {
                FiltroEstado = filtroEstado,
                FiltroSeveridad = filtroSeveridad,
                Estados = Enum.GetNames(typeof(EstadoTicket)).ToList(),
                Severidades = Enum.GetNames(typeof(SeveridadTicket)).ToList(),
                EnCurso = enCurso,
                Resueltos = resueltos,
                Cerrados = sinAsignar,
                Asignados = asignados,
                Pendientes = pendientes
            };

            return View(vm);
        }

        // ============================================================
        //  DETALLES
        // ============================================================
        public async Task<IActionResult> Details(int? id)
        {
            if (!TienePermiso("tickets.ver"))
                return Forbid();

            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                .Include(t => t.UsuarioCreador)
                .Include(t => t.TecnicoAsignado)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound();

            return View(ticket);
        }

        // ============================================================
        //  CREAR
        // ============================================================
        public async Task<IActionResult> Create()
        {
            if (!TienePermiso("tickets.crear"))
                return Forbid();

            await CargarCombosTecnicos();
            ViewBag.Severidades = new SelectList(Enum.GetValues(typeof(SeveridadTicket)));

            return View(new Ticket
            {
                Estado = EstadoTicket.Abierto,
                Severidad = SeveridadTicket.Media
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Titulo,Descripcion,Severidad,TecnicoAsignadoId,Notas")] Ticket ticket)
        {
            if (!TienePermiso("tickets.crear"))
                return Forbid();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            ticket.UsuarioCreadorId = user.Id;
            ticket.Estado = EstadoTicket.Abierto;
            ticket.FechaCreacion = DateTime.UtcNow;

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            TempData["ok"] = "Ticket creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        //  EDITAR
        // ============================================================
        public async Task<IActionResult> Edit(int? id)
        {
            if (!TienePermiso("tickets.editar"))
                return Forbid();

            if (id == null) return NotFound();

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            await CargarCombosTecnicos();

            ViewBag.Severidades = new SelectList(Enum.GetValues(typeof(SeveridadTicket)));
            ViewBag.Estados = new SelectList(Enum.GetValues(typeof(EstadoTicket)));

            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Ticket ticket)
        {
            if (!TienePermiso("tickets.editar"))
                return Forbid();

            if (id != ticket.Id) return NotFound();

            var dbTicket = await _context.Tickets.FirstAsync(t => t.Id == id);

            dbTicket.Titulo = ticket.Titulo;
            dbTicket.Descripcion = ticket.Descripcion;
            dbTicket.Estado = ticket.Estado;
            dbTicket.Severidad = ticket.Severidad;
            dbTicket.TecnicoAsignadoId = ticket.TecnicoAsignadoId;
            dbTicket.Notas = ticket.Notas;

            dbTicket.FechaCierre =
                (dbTicket.Estado == EstadoTicket.Resuelto || dbTicket.Estado == EstadoTicket.Cerrado)
                    ? DateTime.UtcNow
                    : null;

            await _context.SaveChangesAsync();

            TempData["ok"] = "Ticket actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        //  RESOLVER
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolver(int id)
        {
            if (!TienePermiso("tickets.cerrar"))
                return Forbid();

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            if (string.IsNullOrWhiteSpace(ticket.Notas))
            {
                TempData["err"] = "Debes agregar notas antes de resolver.";
                return RedirectToAction(nameof(Edit), new { id });
            }

            ticket.Estado = EstadoTicket.Resuelto;
            ticket.FechaCierre = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["ok"] = "Ticket marcado como RESUELTO.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ============================================================
        //  ASIGNAR
        // ============================================================
        public async Task<IActionResult> Assign(int id, string? q)
        {
            if (!TienePermiso("tickets.editar"))
                return Forbid();

            var ticket = await _context.Tickets
                .Include(t => t.TecnicoAsignado)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null) return NotFound();

            var vm = new AssignTicketVM
            {
                TicketId = id,
                Query = q,
                CurrentAssignedUserName = ticket.TecnicoAsignado?.UserName
            };

            IQueryable<AppUser> users = _userManager.Users;

            if (!string.IsNullOrWhiteSpace(q))
            {
                var f = q.Trim();

                users = users.Where(u =>
                    EF.Functions.Like(u.NombreUsuario ?? "", $"%{f}%") ||
                    EF.Functions.Like(u.UserName ?? "", $"%{f}%") ||
                    EF.Functions.Like(u.Email ?? "", $"%{f}%")
                );
            }
            else
            {
                users = users.Where(u => false);
            }

            vm.Options = await users
                .OrderBy(u => u.NombreUsuario ?? u.UserName ?? "")
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = string.IsNullOrEmpty(u.NombreUsuario)
                        ? u.UserName
                        : $"{u.NombreUsuario} ({u.UserName})"
                })
                .ToListAsync();

            return View("Assign", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int id, AssignTicketVM vm, string? quickAssignMe)
        {
            if (!TienePermiso("tickets.editar"))
                return Forbid();

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            if (!string.IsNullOrEmpty(quickAssignMe))
            {
                var me = await _userManager.GetUserAsync(User);
                ticket.TecnicoAsignadoId = me.Id;

                await _context.SaveChangesAsync();

                TempData["ok"] = "Ticket asignado a tu usuario.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(vm.SelectedUserId))
            {
                ModelState.AddModelError(nameof(vm.SelectedUserId), "Debes seleccionar un usuario.");
                return await Assign(id, vm.Query);
            }

            ticket.TecnicoAsignadoId = vm.SelectedUserId;
            await _context.SaveChangesAsync();

            TempData["ok"] = "Ticket asignado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        //  COMBOS TÉCNICOS
        // ============================================================
        private async Task CargarCombosTecnicos()
        {
            var usuarios = await _userManager.Users
                .OrderBy(u => u.UserName)
                .ToListAsync();

            ViewBag.Tecnicos = usuarios
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = string.IsNullOrWhiteSpace(u.NombreUsuario)
                        ? u.UserName
                        : $"{u.NombreUsuario} ({u.UserName})"
                })
                .ToList();
        }
    }
}
