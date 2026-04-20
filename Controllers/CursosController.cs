using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Practica_2.Data;
using Practica_2.Models;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Practica_2.Controllers;

public class CursosController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<IdentityUser> _userManager; 
    private readonly IDistributedCache _cache;

    public CursosController(ApplicationDbContext context, UserManager<IdentityUser> userManager, IDistributedCache cache)
    {
        _context = context;
        _userManager = userManager;
        _cache = cache;
    }

    // GET: Cursos
    public async Task<IActionResult> Index(CursoFilterViewModel filter)
    {
        string cacheKey = "catalogo_cursos_activos";
        List<Curso>? cursosActivos;

        // 1. Intentar obtener el catálogo base desde Redis
        var cachedData = await _cache.GetStringAsync(cacheKey);

        if (!string.IsNullOrEmpty(cachedData))
        {
            // Cache HIT: Deserializar la información
            cursosActivos = JsonSerializer.Deserialize<List<Curso>>(cachedData);
        }
        else
        {
            // Cache MISS: Ir a la BD si no existe o ya pasaron los 60s
            cursosActivos = await _context.Cursos.Where(c => c.Activo).ToListAsync();

            // Guardar en Redis por 60 segundos
            var options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(60));

            await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(cursosActivos), options);
        }

        // 2. Aplicar filtros en memoria (sobre la lista, no sobre la BD)
        var query = cursosActivos.AsQueryable();

        if (filter.MinCreditos < 0 || filter.MaxCreditos < 0)
        {
            ModelState.AddModelError("", "No se aceptan créditos negativos en los filtros.");
        }

        if (ModelState.IsValid)
        {
            if (!string.IsNullOrEmpty(filter.Nombre))
            {
                // Usamos StringComparison.OrdinalIgnoreCase para búsquedas en memoria sin distinguir mayúsculas
                query = query.Where(c => c.Nombre.Contains(filter.Nombre, StringComparison.OrdinalIgnoreCase));
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
                query = query.Where(c => c.HorarioInicio >= filter.Horario.Value);
            }
        }

        filter.Cursos = query.ToList();

        // 3. Calcular inscritos (consultamos la BD para tener los cupos exactos en tiempo real)
        var cursoIds = filter.Cursos.Select(c => c.Id).ToList();
        
        if (cursoIds.Any())
        {
            var inscritosPorCurso = await _context.Matriculas
                .Where(m => cursoIds.Contains(m.CursoId) && m.Estado != MatriculaEstado.Cancelada)
                .GroupBy(m => m.CursoId)
                .Select(g => new { CursoId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CursoId, x => x.Count);

            ViewBag.InscritosDict = inscritosPorCurso;
        }
        else
        {
            ViewBag.InscritosDict = new Dictionary<int, int>();
        }

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

        HttpContext.Session.SetString("UltimoCursoNombre", curso.Nombre);
        HttpContext.Session.SetInt32("UltimoCursoId", curso.Id);

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