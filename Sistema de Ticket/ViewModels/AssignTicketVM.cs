namespace Sistema_de_Ticket.ViewModels
{
    public class AssignTicketVM
    {
        public int TicketId { get; set; }
        public string? Query { get; set; }                    // texto del buscador
        public string? SelectedUserId { get; set; }           // usuario elegido
        public string? CurrentAssignedUserName { get; set; }  // para mostrar el actual
        public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Options { get; set; }
            = new();
    }
}
