using Microsoft.AspNetCore.Identity;
using Practica_2.Models;

namespace Practica_2.Data;

public static class DbInitializer
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        context.Database.EnsureCreated();

        // 1. Crear Roles si no existen
        string[] roleNames = { "Coordinador", "Estudiante" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. Crear Usuario Coordinador Semilla
        var adminEmail = "coordinador@uni.edu.pe";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new IdentityUser 
            { 
                UserName = adminEmail, 
                Email = adminEmail, 
                EmailConfirmed = true 
            };
            await userManager.CreateAsync(adminUser, "Admin123!");
            await userManager.AddToRoleAsync(adminUser, "Coordinador");
        }

        // 3. Sembrar Cursos Iniciales
        if (!context.Cursos.Any())
        {
            context.Cursos.AddRange(
                new Curso 
                { 
                    Codigo = "INF101", 
                    Nombre = "Programación I", 
                    Creditos = 4, 
                    CupoMaximo = 30, 
                    HorarioInicio = new TimeSpan(8, 0, 0), 
                    HorarioFin = new TimeSpan(10, 0, 0), 
                    Activo = true 
                },
                new Curso 
                { 
                    Codigo = "INF102", 
                    Nombre = "Base de Datos", 
                    Creditos = 3, 
                    CupoMaximo = 25, 
                    HorarioInicio = new TimeSpan(10, 0, 0), 
                    HorarioFin = new TimeSpan(12, 0, 0), 
                    Activo = true 
                },
                new Curso 
                { 
                    Codigo = "INF103", 
                    Nombre = "Desarrollo Web", 
                    Creditos = 4, 
                    CupoMaximo = 20, 
                    HorarioInicio = new TimeSpan(14, 0, 0), 
                    HorarioFin = new TimeSpan(16, 0, 0), 
                    Activo = true 
                }
            );
            await context.SaveChangesAsync();
        }
    }
}
