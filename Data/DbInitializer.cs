using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Practica_2.Models;

namespace Practica_2.Data;

public static class DbInitializer
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        context.Database.Migrate();

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
        var adminEmail = "coordinador@usmp.pe";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        
        if (adminUser == null)
        {
            adminUser = new IdentityUser 
            { 
                UserName = adminEmail, 
                Email = adminEmail, 
                EmailConfirmed = true 
            };
            
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                // Buscamos el rol de nuevo para asegurar que el ID esté en el contexto actual
                if (await roleManager.RoleExistsAsync("Coordinador"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Coordinador");
                }
            }
        }

        // 3. Sembrar Cursos Iniciales
        if (!context.Cursos.Any())
        {
            Console.WriteLine("--- SEMBRANDO CURSOS EN LA BASE DE DATOS ---");
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
                    CupoMaximo = 1, 
                    HorarioInicio = new TimeSpan(14, 0, 0), 
                    HorarioFin = new TimeSpan(16, 0, 0), 
                    Activo = true 
                },
                new Curso 
                { 
                    Codigo = "INF104", 
                    Nombre = "Ingeniería de Software", 
                    Creditos = 3, 
                    CupoMaximo = 20, 
                    HorarioInicio = new TimeSpan(9, 0, 0), 
                    HorarioFin = new TimeSpan(11, 0, 0), 
                    Activo = true 
                },
                new Curso 
                { 
                    Codigo = "INF105", 
                    Nombre = "Redes I", 
                    Creditos = 4, 
                    CupoMaximo = 15, 
                    HorarioInicio = new TimeSpan(16, 0, 0), 
                    HorarioFin = new TimeSpan(18, 0, 0), 
                    Activo = true 
                }
            );
            await context.SaveChangesAsync();
            Console.WriteLine("--- CURSOS SEMBRADOS CON ÉXITO ---");
        }
        else
        {
            Console.WriteLine("--- LA BASE DE DATOS YA TIENE CURSOS, SALTANDO SEMBRADO ---");
        }
    }
}
