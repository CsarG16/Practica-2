using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Practica_2.Data;
using Practica_2.Models;

namespace Practica_2.Controllers;

[Authorize(Roles = "Coordinador")] // Restricción estricta por Rol
public class CoordinadorController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IDistributedCache _cache;
    private const string CacheKey = "catalogo_cursos_activos";

    public CoordinadorController(ApplicationDbContext context, IDistributedCache cache)
    {
        _context = context;
        _cache = cache;
    }

    // GET: /Coordinador
    public async Task<IActionResult> Index()
    {
        var cursos = await _context.Cursos.ToListAsync();
        return View(cursos);
    }

    // --- CRUD DE CURSOS ---

    public IActionResult CrearCurso() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CrearCurso(Curso curso)
    {
        if (ModelState.IsValid)
        {
            _context.Add(curso);
            await _context.SaveChangesAsync();
            
            // INVALIDAR CACHÉ: Como hay un nuevo curso, el catálogo debe actualizarse
            await _cache.RemoveAsync(CacheKey);
            
            return RedirectToAction(nameof(Index));
        }
        return View(curso);
    }

    public async Task<IActionResult> EditarCurso(int id)
    {
        var curso = await _context.Cursos.FindAsync(id);
        if (curso == null) return NotFound();
        return View(curso);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditarCurso(int id, Curso curso)
    {
        if (id != curso.Id) return NotFound();

        if (ModelState.IsValid)
        {
            _context.Update(curso);
            await _context.SaveChangesAsync();
            
            // INVALIDAR CACHÉ: Al editar, el catálogo en Redis queda obsoleto
            await _cache.RemoveAsync(CacheKey);
            
            return RedirectToAction(nameof(Index));
        }
        return View(curso);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActivo(int id)
    {
        var curso = await _context.Cursos.FindAsync(id);
        if (curso == null) return NotFound();

        curso.Activo = !curso.Activo;
        await _context.SaveChangesAsync();
        
        // INVALIDAR CACHÉ
        await _cache.RemoveAsync(CacheKey);
        
        return RedirectToAction(nameof(Index));
    }

    // --- GESTIÓN DE MATRÍCULAS ---

    public async Task<IActionResult> Matriculas(int id)
    {
        var curso = await _context.Cursos
            .Include(c => c.Matriculas)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (curso == null) return NotFound();
        
        ViewBag.CursoNombre = curso.Nombre;
        return View(curso.Matriculas);
    }

    [HttpPost]
    public async Task<IActionResult> GestionarEstado(int matriculaId, MatriculaEstado nuevoEstado)
    {
        var matricula = await _context.Matriculas.FindAsync(matriculaId);
        if (matricula == null) return NotFound();

        matricula.Estado = nuevoEstado;
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Matriculas), new { id = matricula.CursoId });
    }
}