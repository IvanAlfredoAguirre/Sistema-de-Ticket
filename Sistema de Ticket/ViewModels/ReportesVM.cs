using Microsoft.AspNetCore.Mvc.Rendering;
using Sistema_de_Ticket.Models;
using System;
using System.Collections.Generic;

namespace Sistema_de_Ticket.ViewModels
{
    public class ReportePuntoFecha
    {
        public DateTime Fecha { get; set; }
        public int Cantidad { get; set; }
    }

    public class ReporteTicketTabla
    {
        public string Titulo { get; set; } = "";
        public EstadoTicket Estado { get; set; }
        public SeveridadTicket Severidad { get; set; }
        public string Usuario { get; set; } = "";
        public string Tecnico { get; set; } = "";   // <--- NUEVO
        public DateTime FechaCreacion { get; set; }
    }


    public class ReportesVM
    {
        // Métricas
        public int EnCurso { get; set; }
        public int Resueltos { get; set; }
        public int SinAsignar { get; set; }

        // Serie temporal
        public List<ReportePuntoFecha> SeriePorFecha { get; set; } = new();

        // Tabla
        public List<ReporteTicketTabla> Tickets { get; set; } = new();

        // Filtros seleccionados
        public string? UsuarioId { get; set; }
        public string? TecnicoId { get; set; }
        public SeveridadTicket? Severidad { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }

        // Combos
        public List<SelectListItem> Usuarios { get; set; } = new();
        public List<SelectListItem> Tecnicos { get; set; } = new();
        public List<SelectListItem> Severidades { get; set; } = new();
    }
}
