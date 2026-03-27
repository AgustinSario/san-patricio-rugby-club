using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SanPatricioRugby.DAL.Models
{
    public class Cuota
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SocioId { get; set; }

        [ForeignKey("SocioId")]
        public virtual Socio? Socio { get; set; }

        [Required]
        [Range(2020, 2100)]
        [Display(Name = "Año")]
        public int Anio { get; set; }

        [Required]
        [Range(1, 12)]
        public int Mes { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Monto { get; set; }

        [Display(Name = "Estado")]
        public EstadoPago Estado { get; set; } = EstadoPago.Pendiente;

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Vencimiento")]
        public DateTime FechaVencimiento { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Pago")]
        public DateTime? FechaPago { get; set; }

        [Display(Name = "Pagado con")]
        public MedioPago? MedioPagoUtilizado { get; set; }
    }
}
