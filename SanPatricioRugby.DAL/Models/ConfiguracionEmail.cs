using System.ComponentModel.DataAnnotations;

namespace SanPatricioRugby.DAL.Models
{
    public class ConfiguracionEmail
    {
        [Key]
        public int Id { get; set; }

        // ── Datos del remitente / SMTP ────────────────────────────────────────
        [Required(ErrorMessage = "El email remitente es obligatorio")]
        [Display(Name = "Email Remitente")]
        [EmailAddress]
        public string EmailRemitente { get; set; } = null!;

        [Display(Name = "Nombre Remitente")]
        public string NombreRemitente { get; set; } = "San Patricio Rugby Club";

        [Required(ErrorMessage = "El servidor SMTP es obligatorio")]
        [Display(Name = "Servidor SMTP")]
        public string SmtpHost { get; set; } = "smtp.gmail.com";

        [Display(Name = "Puerto SMTP")]
        public int SmtpPort { get; set; } = 587;

        [Display(Name = "Usuario SMTP")]
        public string SmtpUser { get; set; } = null!;

        [Display(Name = "Contraseña SMTP")]
        public string SmtpPassword { get; set; } = null!;

        [Display(Name = "Usar SSL/TLS")]
        public bool UsarSsl { get; set; } = true;

        // ── Datos del club (para el recibo PDF) ───────────────────────────────
        [Display(Name = "Nombre del Club")]
        public string NombreClub { get; set; } = "SAN PATRICIO RUGBY CLUB";

        [Display(Name = "Razón Social")]
        public string RazonSocial { get; set; } = "SAN PATRICIO RUGBY CLUB";

        [Display(Name = "Domicilio Comercial")]
        public string Domicilio { get; set; } = "Santa Fe 634 - Corrientes, Corrientes";

        [Display(Name = "CUIT")]
        public string Cuit { get; set; } = "30-68793724-0";

        [Display(Name = "Ingresos Brutos")]
        public string IngresosBrutos { get; set; } = "30687937240";

        [Display(Name = "Fecha de Inicio de Actividades")]
        public string InicioActividades { get; set; } = "28/02/1991";

        [Display(Name = "Condición frente al IVA")]
        public string CondicionIva { get; set; } = "IVA Sujeto Exento";
    }
}
