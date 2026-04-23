using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SanPatricioRugby.DAL.Models
{
    public class GrupoFamiliar
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del grupo es obligatorio")]
        [Display(Name = "Nombre del Grupo Familiar")]
        public string Nombre { get; set; } = null!;

        public virtual ICollection<Socio> Miembros { get; set; } = new List<Socio>();
    }
}
