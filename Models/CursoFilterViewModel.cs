namespace Practica_2.Models;

public class CursoFilterViewModel
{
    // Filtros
    public string? Nombre { get; set; }
    public int? MinCreditos { get; set; }
    public int? MaxCreditos { get; set; }
    public TimeSpan? Horario { get; set; }

    // Resultados
    public List<Curso> Cursos { get; set; } = new();
}
