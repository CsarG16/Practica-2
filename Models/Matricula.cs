using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Practica_2.Models;

public class Matricula
{
    public int Id { get; set; }

    [Required]
    public int CursoId { get; set; }
    public Curso? Curso { get; set; }

    [Required]
    public string UsuarioId { get; set; } = string.Empty;
    public IdentityUser? Usuario { get; set; }

    [Display(Name = "Fecha de Registro")]
    public DateTime FechaRegistro { get; set; } = DateTime.Now;

    public MatriculaEstado Estado { get; set; } = MatriculaEstado.Pendiente;
}
