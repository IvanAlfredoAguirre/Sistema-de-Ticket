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

        // ============ LISTADO ============
        public async Task<IActionResult> Index(string? filtroEstado, string? filtroSeveridad)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var roles = await _userManager.GetRolesAsync(user);

            IQueryable<Ticket> q = _context.Tickets
                .Include(t => t.UsuarioCreador)
                .Include(t => t.TecnicoAsignado);

            if (!roles.Contains("SuperAdmin"))
            {
                if (roles.Contains("Técnico") || roles.Contains("Tecnico") || roles.Contains("Soporte"))
                    q = q.Where(t => t.TecnicoAsignadoId == user.Id);
                else
                    q = q.Where(t => t.UsuarioCreadorId == user.Id);
            }

            if (!string.IsNullOrWhiteSpace(filtroEstado) &&
                Enum.TryParse<EstadoTicket>(filtroEstado, true, out var est))
                q = q.Where(t => t.Estado == est);

            if (!string.IsNullOrWhiteSpace(filtroSeveridad) &&
                Enum.TryParse<SeveridadTicket>(filtroSeveridad, true, out var sev))
                q = q.Where(t => t.Severidad == sev);

            var enCurso = await q.CountAsync(t => t.Estado == EstadoTicket.Abierto || t.Estado == EstadoTicket.EnProceso);
            var resueltos = await q.CountAsync(t => t.Estado == EstadoTicket.Resuelto);
            var sinAsignar = await q.CountAsync(t => t.TecnicoAsignadoId == null);

            var asignados = await q.Where(t => t.TecnicoAsignadoId != null)
                                   .OrderByDescending(t => t.FechaCreacion)
                                   .ToListAsync();

            var pendientes = await q.Where(t => t.TecnicoAsignadoId == null)
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

        // ============ DETALLES ============
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                .Include(t => t.UsuarioCreador)
                .Include(t => t.TecnicoAsignado)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null) return NotFound();

            return View(ticket);
        }

        // ============ CREAR ============
        public async Task<IActionResult> Create()
        {
            await CargarCombosTecnicos();
            ViewBag.Severidades = new SelectList(Enum.GetValues(typeof(SeveridadTicket)));
            return View(new Ticket { Estado = EstadoTicket.Abierto, Severidad = SeveridadTicket.Media });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Titulo,Descripcion,Severidad,TecnicoAsignadoId,Notas")] Ticket ticket)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Ignorar campos seteados por el server
            ModelState.Remove(nameof(Ticket.UsuarioCreadorId));
            ModelState.Remove(nameof(Ticket.Estado));
            ModelState.Remove(nameof(Ticket.FechaCreacion));
            ModelState.Remove(nameof(Ticket.FechaCierre));

            if (!ModelState.IsValid)
            {
                await CargarCombosTecnicos();
                ViewBag.Severidades = new SelectList(Enum.GetValues(typeof(SeveridadTicket)));
                return View(ticket);
            }

            ticket.UsuarioCreadorId = user.Id;
            ticket.Estado = EstadoTicket.Abierto;
            ticket.FechaCreacion = DateTime.UtcNow;

            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();

            TempData["ok"] = "Ticket creado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // ============ EDITAR ============
        public async Task<IActionResult> Edit(int? id)
        {
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Titulo,Descripcion,Estado,Severidad,TecnicoAsignadoId,Notas")] Ticket ticket)
        {
            if (id != ticket.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                await CargarCombosTecnicos();
                ViewBag.Severidades = new SelectList(Enum.GetValues(typeof(SeveridadTicket)));
                ViewBag.Estados = new SelectList(Enum.GetValues(typeof(EstadoTicket)));
                return View(ticket);
            }

            var dbTicket = await _context.Tickets.FirstAsync(t => t.Id == id);
            dbTicket.Titulo = ticket.Titulo;
            dbTicket.Descripcion = ticket.Descripcion;
            dbTicket.Estado = ticket.Estado;
            dbTicket.Severidad = ticket.Severidad;
            dbTicket.TecnicoAsignadoId = ticket.TecnicoAsignadoId;
            dbTicket.Notas = ticket.Notas;
            dbTicket.FechaCierre = (dbTicket.Estado == EstadoTicket.Cerrado) ? DateTime.UtcNow : null;

            await _context.SaveChangesAsync();
            TempData["ok"] = "Ticket actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // ============ RESOLVER ============
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolver(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            if (ticket.Estado != EstadoTicket.Resuelto)
            {
                ticket.Estado = EstadoTicket.Resuelto;
                ticket.FechaCierre = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["ok"] = "Ticket marcado como RESUELTO.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // ============ ASIGNAR ============
        [HttpGet]
        public async Task<IActionResult> Assign(int id, string? q)
        {
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

            // Buscar en TODOS los usuarios
            IQueryable<AppUser> users = _userManager.Users;

            if (!string.IsNullOrWhiteSpace(q))
            {
                var f = q.Trim();
                users = users.Where(u =>
                    (u.NombreUsuario != null && EF.Functions.Like(u.NombreUsuario, $"%{f}%")) ||
                    (u.UserName != null && EF.Functions.Like(u.UserName, $"%{f}%")) ||
                    (u.Email != null && EF.Functions.Like(u.Email, $"%{f}%"))
                );
            }
            else
            {
                users = users.Where(u => false); // sin query, no listamos todo
            }

            vm.Options = await users
                .OrderBy(u => u.NombreUsuario ?? u.UserName ?? string.Empty)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = string.IsNullOrWhiteSpace(u.NombreUsuario)
                            ? (u.UserName ?? "")
                            : $"{u.NombreUsuario} ({u.UserName})"
                })
                .ToListAsync();

            return View("Assign", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(int id, AssignTicketVM vm, string? quickAssignMe)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null) return NotFound();

            // Asignarme a mí
            if (!string.IsNullOrEmpty(quickAssignMe))
            {
                var me = await _userManager.GetUserAsync(User);
                if (me == null) return Challenge();

                ticket.TecnicoAsignadoId = me.Id;
                await _context.SaveChangesAsync();
                TempData["ok"] = "Ticket asignado a tu usuario.";
                return RedirectToAction(nameof(Index));
            }

            // Asignar usuario elegido
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

        // ============ ELIMINAR ============
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.Tickets
                .Include(t => t.UsuarioCreador)
                .Include(t => t.TecnicoAsignado)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (ticket == null) return NotFound();

            return View(ticket);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                _context.Tickets.Remove(ticket);
                await _context.SaveChangesAsync();
            }
            TempData["ok"] = "Ticket eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private async Task CargarCombosTecnicos()
        {
            var rolesValidos = new[] { "Técnico", "Tecnico", "Soporte" };
            var usuarios = new List<AppUser>();

            foreach (var rol in rolesValidos)
            {
                var enRol = await _userManager.GetUsersInRoleAsync(rol);
                usuarios.AddRange(enRol);
            }

            ViewBag.Tecnicos = usuarios.GroupBy(u => u.Id).Select(g => g.First())
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = string.IsNullOrWhiteSpace(u.NombreUsuario)
                        ? (u.UserName ?? "")
                        : $"{u.NombreUsuario} ({u.UserName})"
                })
                .ToList();
        }
    }
}
