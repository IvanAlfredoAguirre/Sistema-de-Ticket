using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Ticket.Data;
using Sistema_de_Ticket.Models;
using Sistema_de_Ticket.ViewModels;
using Sistema_de_Ticket.Auth;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Sistema_de_Ticket.Controllers
{
    [Authorize]
    public class ReportesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public ReportesController(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private bool TienePermiso(string permiso)
        {
            if (User.IsInRole("SuperAdmin"))
                return true;

            return User.HasClaim(AuthorizationConfig.PermissionClaimType, permiso);
        }

        // ============================================================
        // INDEX (Ver reportes)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index(
            string? usuarioId,
            string? tecnicoId,
            SeveridadTicket? severidad,
            DateTime? desde,
            DateTime? hasta)
        {
            if (!TienePermiso(Permisos.ReportesVer))
                return Forbid();

            // PERMISO PARA EXPORTAR (pasamos a la vista)
            ViewBag.PuedeExportar = TienePermiso(Permisos.ReportesExportar);

            var q = _context.Tickets
                .Include(t => t.UsuarioCreador)
                .Include(t => t.TecnicoAsignado)
                .AsQueryable();

            if (!string.IsNullOrEmpty(usuarioId))
                q = q.Where(t => t.UsuarioCreadorId == usuarioId);

            if (!string.IsNullOrEmpty(tecnicoId))
                q = q.Where(t => t.TecnicoAsignadoId == tecnicoId &&
                                 t.Estado == EstadoTicket.Resuelto);

            if (severidad.HasValue)
                q = q.Where(t => t.Severidad == severidad.Value);

            if (desde.HasValue)
                q = q.Where(t => t.FechaCreacion >= desde.Value);

            if (hasta.HasValue)
            {
                var fh = hasta.Value.Date.AddDays(1);
                q = q.Where(t => t.FechaCreacion < fh);
            }

            var enCurso = await q.CountAsync(t => t.Estado == EstadoTicket.Abierto || t.Estado == EstadoTicket.EnProceso);
            var resueltos = await q.CountAsync(t => t.Estado == EstadoTicket.Resuelto);
            var sinAsignar = await q.CountAsync(t => t.TecnicoAsignadoId == null);

            var serie = await q
                .GroupBy(t => t.FechaCreacion.Date)
                .Select(g => new ReportePuntoFecha
                {
                    Fecha = g.Key,
                    Cantidad = g.Count()
                })
                .OrderBy(x => x.Fecha)
                .ToListAsync();

            var tabla = await q
                .OrderByDescending(t => t.FechaCreacion)
                .Take(300)
                .Select(t => new ReporteTicketTabla
                {
                    Titulo = t.Titulo,
                    Estado = t.Estado,
                    Severidad = t.Severidad,
                    Usuario = t.UsuarioCreador != null ? t.UsuarioCreador.UserName : "(sin usuario)",
                    Tecnico = t.TecnicoAsignado != null ? t.TecnicoAsignado.UserName : "Sin asignar",
                    FechaCreacion = t.FechaCreacion
                })
                .ToListAsync();

            var usuarios = await _userManager.Users
                .OrderBy(u => u.UserName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.UserName,
                    Selected = u.Id == usuarioId
                })
                .ToListAsync();

            var tecnicos = await _userManager.Users
                .Where(u => _context.Tickets.Any(t => t.TecnicoAsignadoId == u.Id))
                .OrderBy(u => u.UserName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.UserName,
                    Selected = u.Id == tecnicoId
                })
                .ToListAsync();

            var severidades = Enum.GetValues(typeof(SeveridadTicket))
                .Cast<SeveridadTicket>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.ToString(),
                    Selected = s == severidad
                })
                .ToList();

            return View(new ReportesVM
            {
                EnCurso = enCurso,
                Resueltos = resueltos,
                SinAsignar = sinAsignar,
                SeriePorFecha = serie,
                Tickets = tabla,
                UsuarioId = usuarioId,
                TecnicoId = tecnicoId,
                Severidad = severidad,
                Desde = desde,
                Hasta = hasta,
                Usuarios = usuarios,
                Tecnicos = tecnicos,
                Severidades = severidades
            });
        }
    }
}
