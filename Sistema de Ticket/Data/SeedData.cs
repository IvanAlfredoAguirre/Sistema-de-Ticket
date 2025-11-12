using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Sistema_de_Ticket.Models;
using Sistema_de_Ticket.Auth;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;

namespace Sistema_de_Ticket.Data
{
    public static class SeedData
    {
        public static async Task InicializarAsync(IServiceProvider serviceProvider)
        {
            // Servicios necesarios
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

            // Roles base del sistema
            string[] roles = new[] { "SuperAdmin", "Admin", "Soporte", "Usuario" };

            foreach (var rol in roles)
            {
                if (!await roleManager.RoleExistsAsync(rol))
                {
                    await roleManager.CreateAsync(new IdentityRole(rol));
                }
            }

            // Usuario SuperAdmin (semilla inicial)
            string usuarioAdmin = "admin";
            string passwordAdmin = "Admin123*";

            var user = await userManager.FindByNameAsync(usuarioAdmin);
            if (user == null)
            {
                user = new AppUser
                {
                    UserName = usuarioAdmin,
                    NombreUsuario = "Administrador del sistema",
                    Email = "admin@soporte.local",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, passwordAdmin);
                if (!result.Succeeded)
                {
                    throw new Exception("Error creando el usuario admin: " +
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // Asignar rol al admin si no lo tiene
            if (!await userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                await userManager.AddToRoleAsync(user, "SuperAdmin");
            }

            // Asegurar que el rol SuperAdmin tenga todos los permisos
            var role = await roleManager.FindByNameAsync("SuperAdmin");
            if (role != null)
            {
                var existingClaims = await roleManager.GetClaimsAsync(role);
                var existentes = existingClaims
                    .Where(c => c.Type == AuthorizationConfig.PermissionClaimType)
                    .Select(c => c.Value)
                    .ToHashSet();

                foreach (var permiso in Permisos.Todos)
                {
                    if (!existentes.Contains(permiso))
                    {
                        await roleManager.AddClaimAsync(
                            role,
                            new Claim(AuthorizationConfig.PermissionClaimType, permiso)
                        );
                    }
                }
            }
        }
    }
}
