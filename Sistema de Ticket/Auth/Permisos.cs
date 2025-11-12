namespace Sistema_de_Ticket.Auth
{
    public static class Permisos
    {
        public const string VerUsuarios = "usuarios.ver";
        public const string CrearUsuario = "usuarios.crear";
        public const string EditarUsuario = "usuarios.editar";
        public const string EliminarUsuario = "usuarios.eliminar";

        public const string VerTickets = "tickets.ver";
        public const string CrearTicket = "tickets.crear";
        public const string EditarTicket = "tickets.editar";
        public const string CerrarTicket = "tickets.cerrar";
        public const string EliminarTicket = "tickets.eliminar";

        public const string VerNotificaciones = "notificaciones.ver";
        public const string CrearNotificacion = "notificaciones.crear";
        public const string EditarNotificacion = "notificaciones.editar";
        public const string EliminarNotificacion = "notificaciones.eliminar";

        public static readonly string[] Todos =
        {
            VerUsuarios, CrearUsuario, EditarUsuario, EliminarUsuario,
            VerTickets, CrearTicket, EditarTicket, CerrarTicket, EliminarTicket,
            VerNotificaciones, CrearNotificacion, EditarNotificacion, EliminarNotificacion
        };
    }
}

