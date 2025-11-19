using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Ticket.Data;
using Sistema_de_Ticket.Models;
using Sistema_de_Ticket.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sistema_de_Ticket.Controllers
{
    // Luego puedes cambiar esto por una policy tipo [Authorize(Policy = Permisos.ReportesVer)]
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

        [HttpGet]
        public async Task<IActionResult> Index(
            string? usuarioId,
            string? tecnicoId,
            SeveridadTicket? severidad,
            DateTime? desde,
            DateTime? hasta)
        {
            // Query base
            var q = _context.Tickets
                .Include(t => t.UsuarioCreador)
                .Include(t => t.TecnicoAsignado)
                .AsQueryable();

            // Filtro por usuario (creador)
            if (!string.IsNullOrEmpty(usuarioId))
            {
                q = q.Where(t => t.UsuarioCreadorId == usuarioId);
            }

            // Filtro por técnico asignado -> SOLO tickets RESUELTOS de ese técnico
            if (!string.IsNullOrEmpty(tecnicoId))
            {
                q = q.Where(t => t.TecnicoAsignadoId == tecnicoId
                                 && t.Estado == EstadoTicket.Resuelto);
            }

            // Filtro por severidad
            if (severidad.HasValue)
            {
                q = q.Where(t => t.Severidad == severidad.Value);
            }

            // Filtro por fecha
            if (desde.HasValue)
            {
                q = q.Where(t => t.FechaCreacion >= desde.Value);
            }

            if (hasta.HasValue)
            {
                var hastaIncl = hasta.Value.Date.AddDays(1); // incluir todo el día
                q = q.Where(t => t.FechaCreacion < hastaIncl);
            }

            // Métricas para gráfico de torta
            var enCurso = await q.CountAsync(t =>
                t.Estado == EstadoTicket.Abierto ||
                t.Estado == EstadoTicket.EnProceso);

            var resueltos = await q.CountAsync(t => t.Estado == EstadoTicket.Resuelto);
            var sinAsignar = await q.CountAsync(t => t.TecnicoAsignadoId == null);

            // Serie temporal tickets por fecha
            var serie = await q
                .GroupBy(t => t.FechaCreacion.Date)
                .Select(g => new ReportePuntoFecha
                {
                    Fecha = g.Key,
                    Cantidad = g.Count()
                })
                .OrderBy(p => p.Fecha)
                .ToListAsync();

            // Datos tabla inferior
            var ticketsTabla = await q
                .OrderByDescending(t => t.FechaCreacion)
                .Take(300)
                .Select(t => new ReporteTicketTabla
                {
                    Titulo = t.Titulo,
                    Estado = t.Estado,
                    Severidad = t.Severidad,

                    // Si no hay creador, mostramos texto “(sin usuario)”
                    Usuario = t.UsuarioCreador != null
                                ? (t.UsuarioCreador.UserName ?? "(sin usuario)")
                                : "(sin usuario)",

                    // Si no hay técnico, “Sin asignar”
                    Tecnico = t.TecnicoAsignado != null
                                ? (t.TecnicoAsignado.UserName ?? "Sin asignar")
                                : "Sin asignar",

                    FechaCreacion = t.FechaCreacion
                })
                .ToListAsync();

            // Combo de usuarios (creadores)
            var usuarios = await _userManager.Users
                .OrderBy(u => u.UserName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.UserName ?? "(sin nombre)",
                    Selected = (u.Id == usuarioId)
                })
                .ToListAsync();

            // Combo de técnicos: todos los usuarios que alguna vez fueron técnico asignado
            var tecnicos = await _userManager.Users
                .Where(u => _context.Tickets.Any(t => t.TecnicoAsignadoId == u.Id))
                .OrderBy(u => u.UserName)
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = u.UserName ?? "(sin nombre)",
                    Selected = (u.Id == tecnicoId)
                })
                .ToListAsync();

            // Combo de severidades
            var severidades = Enum.GetValues(typeof(SeveridadTicket))
                .Cast<SeveridadTicket>()
                .Select(s => new SelectListItem
                {
                    Value = s.ToString(),
                    Text = s.ToString(),
                    Selected = severidad.HasValue && s == severidad.Value
                })
                .ToList();

            var vm = new ReportesVM
            {
                EnCurso = enCurso,
                Resueltos = resueltos,
                SinAsignar = sinAsignar,
                SeriePorFecha = serie,
                Tickets = ticketsTabla,
                UsuarioId = usuarioId,
                TecnicoId = tecnicoId,
                Severidad = severidad,
                Desde = desde,
                Hasta = hasta,
                Usuarios = usuarios,
                Tecnicos = tecnicos,
                Severidades = severidades
            };

            return View(vm);
        }
    }
}
