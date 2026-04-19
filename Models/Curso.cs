using System.ComponentModel.DataAnnotations;

namespace Practica_2.Models;

public class Curso
{
    public int Id { get; set; }

    [Required(ErrorMessage = "El código es obligatorio")]
    [StringLength(10)]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Los créditos deben ser mayores que 0")]
    public int Creditos { get; set; }

    [Display(Name = "Cupo Máximo")]
    [Range(1, 100)]
    public int CupoMaximo { get; set; }

    [Display(Name = "Horario Inicio")]
    [DataType(DataType.Time)]
    public TimeSpan HorarioInicio { get; set; }

    [Display(Name = "Horario Fin")]
    [DataType(DataType.Time)]
    public TimeSpan HorarioFin { get; set; }

    public bool Activo { get; set; } = true;
}
