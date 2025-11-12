using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Sistema_de_Ticket.Data;
using Sistema_de_Ticket.Models;
using Sistema_de_Ticket.Auth; // <-- nuevo

// 1) EF Core (SQL Server)
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("AppDb")));

// 2) Identity (usuarios, roles, login, contraseñas)
builder.Services
    .AddIdentity<AppUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddDefaultUI();

// 2.1) Authorization con policies por permiso
builder.Services.AddAuthorization(options =>
{
    AuthorizationConfig.RegisterPolicies(options);
});

// 3) MVC
builder.Services.AddControllersWithViews();

// Crear app
var app = builder.Build();

// 4) Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Tickets/Index");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 5) Rutas
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Tickets}/{action=Index}/{id?}"
);

app.MapRazorPages();

// 6) Seed de SuperAdmin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedData.InicializarAsync(services);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al crear usuario admin: {ex.Message}");
    }
}

await app.RunAsync();
