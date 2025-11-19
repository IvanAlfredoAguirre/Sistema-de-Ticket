namespace Sistema_de_Ticket.Auth
{
    /// <summary>
    /// Catálogo central de todos los permisos del sistema.
    /// </summary>
    public static class Permisos
    {
        // ==== USUARIOS ====
        public const string UsuariosVer = "usuarios.ver";
        public const string UsuariosCrear = "usuarios.crear";
        public const string UsuariosEditar = "usuarios.editar";
        public const string UsuariosEliminar = "usuarios.eliminar";

        // ==== TICKETS ====
        public const string TicketsVer = "tickets.ver";
        public const string TicketsCrear = "tickets.crear";
        public const string TicketsEditar = "tickets.editar";
        public const string TicketsCerrar = "tickets.cerrar";
        public const string TicketsEliminar = "tickets.eliminar";

        // ==== ROLES ====
        public const string RolesVer = "roles.ver";
        public const string RolesCrear = "roles.crear";
        public const string RolesEditar = "roles.editar";
        public const string RolesEliminar = "roles.eliminar";

        // ==== REPORTES ====
        public const string ReportesVer = "reportes.ver";
        public const string ReportesExportar = "reportes.exportar";

        // ==== NOTIFICACIONES ====
        public const string NotificacionesVer = "notificaciones.ver";
        public const string NotificacionesCrear = "notificaciones.crear";
        public const string NotificacionesEditar = "notificaciones.editar";
        public const string NotificacionesEliminar = "notificaciones.eliminar";

        /// <summary>
        /// Lista con **todos** los códigos de permiso.
        /// La usa RolesController para armar la pantalla de checkboxes
        /// y AuthorizationConfig para registrar policies.
        /// </summary>
        public static readonly string[] Todos = new[]
        {
            // Usuarios
            UsuariosVer, UsuariosCrear, UsuariosEditar, UsuariosEliminar,

            // Tickets
            TicketsVer, TicketsCrear, TicketsEditar, TicketsCerrar, TicketsEliminar,

            // Roles
            RolesVer, RolesCrear, RolesEditar, RolesEliminar,

            // Reportes
            ReportesVer, ReportesExportar,

            // Notificaciones
            NotificacionesVer, NotificacionesCrear, NotificacionesEditar, NotificacionesEliminar
        };
    }
}

