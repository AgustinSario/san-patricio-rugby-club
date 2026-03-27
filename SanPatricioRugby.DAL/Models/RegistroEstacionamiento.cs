using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanPatricioRugby.DAL.Models
{
    public class RegistroEstacionamiento
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        [Required]
        public TipoVehiculo Vehiculo { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoPagado { get; set; }

        public string? Patente { get; set; }
    }
}
