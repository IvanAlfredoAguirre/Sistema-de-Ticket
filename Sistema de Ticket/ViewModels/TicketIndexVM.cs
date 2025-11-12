using System.Collections.Generic;
using Sistema_de_Ticket.Models;

namespace Sistema_de_Ticket.ViewModels
{
    public class TicketIndexVM
    {
        // Filtros seleccionados
        public string? FiltroEstado { get; set; }
        public string? FiltroSeveridad { get; set; }

        // Catálogos para los selects
        public List<string> Estados { get; set; } = new();
        public List<string> Severidades { get; set; } = new();

        // Métricas superiores
        public int EnCurso { get; set; }     // Abierto + EnProceso
        public int Resueltos { get; set; }   // si no usás "Resuelto", dejalo en 0
        public int Cerrados { get; set; }

        // Listas de tablas
        public List<Ticket> Asignados { get; set; } = new();
        public List<Ticket> Pendientes { get; set; } = new();
    }
}
