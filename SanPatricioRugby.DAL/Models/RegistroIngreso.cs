using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanPatricioRugby.DAL.Models
{
    public class RegistroIngreso
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.Now;

        public int? SocioId { get; set; }

        [ForeignKey("SocioId")]
        public virtual Socio? Socio { get; set; }

        [Required]
        public TipoIngreso Tipo { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MontoPagado { get; set; }

        public string? Observaciones { get; set; }
    }
}
