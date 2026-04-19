using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Practica_2.Data;
using Practica_2.Models;

namespace Practica_2.Controllers;

public class CursosController : Controller
{
    private readonly ApplicationDbContext _context;

    public CursosController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Cursos
    public async Task<IActionResult> Index(CursoFilterViewModel filter)
    {
        // Consulta base: solo cursos activos
        var query = _context.Cursos.Where(c => c.Activo).AsQueryable();

        // Validaciones server-side de filtros (Pregunta 2)
        if (filter.MinCreditos < 0 || filter.MaxCreditos < 0)
        {
            ModelState.AddModelError("", "No se aceptan créditos negativos en los filtros.");
        }

        // Aplicar filtros si son válidos
        if (ModelState.IsValid)
        {
            if (!string.IsNullOrEmpty(filter.Nombre))
            {
                query = query.Where(c => c.Nombre.ToLower().Contains(filter.Nombre.ToLower()));
            }

            if (filter.MinCreditos.HasValue)
            {
                query = query.Where(c => c.Creditos >= filter.MinCreditos.Value);
            }

            if (filter.MaxCreditos.HasValue)
            {
                query = query.Where(c => c.Creditos <= filter.MaxCreditos.Value);
            }

            if (filter.Horario.HasValue)
            {
                // Busca cursos que empiecen en o después de la hora indicada
                query = query.Where(c => c.HorarioInicio >= filter.Horario.Value);
            }
        }

        filter.Cursos = await query.ToListAsync();
        return View(filter);
    }

    // GET: Cursos/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var curso = await _context.Cursos
            .FirstOrDefaultAsync(m => m.Id == id);

        if (curso == null)
        {
            return NotFound();
        }

        return View(curso);
    }
}
