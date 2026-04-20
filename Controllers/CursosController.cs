using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Practica_2.Data;
using Practica_2.Models;

namespace Practica_2.Controllers;

public class CursosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager; // 1. Agregamos el UserManager

    // 2. Lo inyectamos en el constructor
    public CursosController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
    {
        _context = context;
        _userManager = userManager;
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

        // Calcular inscritos por curso para mostrar vacantes en el catálogo
        var cursoIds = filter.Cursos.Select(c => c.Id).ToList();
        var inscritosPorCurso = await _context.Matriculas
            .Where(m => cursoIds.Contains(m.CursoId) && m.Estado != MatriculaEstado.Cancelada)
            .GroupBy(m => m.CursoId)
            .Select(g => new { CursoId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CursoId, x => x.Count);

        ViewBag.InscritosDict = inscritosPorCurso;

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

        // 1. Contamos las matrículas que NO estén canceladas para este curso
        var inscritos = await _context.Matriculas
            .CountAsync(m => m.CursoId == id && m.Estado != MatriculaEstado.Cancelada);

        // 2. Calculamos los cupos restantes y los pasamos por ViewBag
        ViewBag.CuposLibres = curso.CupoMaximo - inscritos;

        return View(curso);
    }

    [Authorize] // Solo usuarios autenticados
    [HttpPost]
    public async Task<IActionResult> Inscribirse(int id)
    {
        var usuario = await _userManager.GetUserAsync(User);
        if (usuario == null) return Challenge();

        var curso = await _context.Cursos.FindAsync(id);
        if (curso == null || !curso.Activo)
        {
            TempData["Error"] = "El curso no existe o no está activo.";
            return RedirectToAction(nameof(Index)); 
        }

        // Validar si ya está matriculado (usando el enum MatriculaEstado)
        var yaMatriculado = await _context.Matriculas
            .AnyAsync(m => m.CursoId == id && m.UsuarioId == usuario.Id && m.Estado != MatriculaEstado.Cancelada);
        
        if (yaMatriculado)
        {
            TempData["Error"] = "Ya estás matriculado en este curso.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Validar Cupo Máximo (usando el enum MatriculaEstado)
        var matriculadosActuales = await _context.Matriculas
            .CountAsync(m => m.CursoId == id && m.Estado != MatriculaEstado.Cancelada);

        if (matriculadosActuales >= curso.CupoMaximo)
        {
            TempData["Error"] = "El curso ha alcanzado el cupo máximo.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // 5. Validar Solapamiento de Horarios (Evaluación en cliente)
        // Primero, traemos las matrículas del usuario a memoria con ToListAsync()
        var misMatriculas = await _context.Matriculas
            .Include(m => m.Curso)
            .Where(m => m.UsuarioId == usuario.Id && m.Estado != MatriculaEstado.Cancelada)
            .ToListAsync();

        // Luego, usamos .Any() normal (no AnyAsync) para evaluar los horarios en C#
        var solapamiento = misMatriculas.Any(m => 
            (curso.HorarioInicio >= m.Curso!.HorarioInicio && curso.HorarioInicio < m.Curso!.HorarioFin) ||
            (curso.HorarioFin > m.Curso!.HorarioInicio && curso.HorarioFin <= m.Curso!.HorarioFin) ||
            (curso.HorarioInicio <= m.Curso!.HorarioInicio && curso.HorarioFin >= m.Curso!.HorarioFin));

        if (solapamiento)
        {
            TempData["Error"] = "El horario de este curso se solapa con otro curso en el que ya estás inscrito.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Registrar Matrícula asignando el enum
        var nuevaMatricula = new Matricula
        {
            CursoId = id,
            UsuarioId = usuario.Id,
            FechaRegistro = DateTime.UtcNow,
            Estado = MatriculaEstado.Pendiente 
        };

        _context.Matriculas.Add(nuevaMatricula);
        await _context.SaveChangesAsync();

        TempData["Exito"] = "Inscripción realizada con éxito. Tu estado es: Pendiente.";
        return RedirectToAction(nameof(Details), new { id });
    }
}