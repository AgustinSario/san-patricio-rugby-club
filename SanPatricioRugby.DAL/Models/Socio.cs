using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SanPatricioRugby.DAL.Models
{
    public class Socio
    {
        [Key]
        public int Id { get; set; }

        [Display(Name = "Nro. Identificador")]
        public string? NumeroIdentificador { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Apellido y Nombre")]
        public string ApellidoNombre { get; set; } = null!;

        [Display(Name = "DNI")]
        public string? Dni { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fecha de Nacimiento")]
        public DateTime? FechaNacimiento { get; set; }

        public string? Sexo { get; set; }
        public string? Celular { get; set; }

        [Display(Name = "Tipo de Socio")]
        public string? TipoSocio { get; set; }
        
        public string? Deporte { get; set; }
        public string? Division { get; set; }
        public string? Camada { get; set; }

        [Display(Name = "Medio de Pago")]
        public MedioPago MedioPagoPredeterminado { get; set; } = MedioPago.NoRegistrado;

        [Display(Name = "Nro. Tarjeta (Oculto)")]
        public string? NumeroTarjeta { get; set; }

        [Display(Name = "Titular de Tarjeta")]
        public string? NombreTitularTarjeta { get; set; }

        [Display(Name = "Acuerdos")]
        public string? Acuerdos { get; set; }

        [Display(Name = "Fecha Nacimiento 2")]
        public DateTime? FechaNacimiento2 { get; set; }

        public bool EsActivo { get; set; } = true;

        [Display(Name = "Email")]
        [EmailAddress]
        public string? Email { get; set; }

        [Display(Name = "Foto del Socio")]
        public string? FotoPath { get; set; }

        [Display(Name = "Ruta del Carnet")]
        public string? CarnetPath { get; set; }

        public virtual ICollection<Cuota> Cuotas { get; set; } = new List<Cuota>();
    }
}
