using System.ComponentModel.DataAnnotations;

namespace SanPatricioRugby.DAL.Models
{
    public enum MedioPago
    {
        [Display(Name = "No Registrado")]
        NoRegistrado = 0,
        [Display(Name = "Efectivo")]
        Efectivo = 1,
        [Display(Name = "Débito")]
        Debito = 2,
        [Display(Name = "Transferencia")]
        Transferencia = 3,
        [Display(Name = "Tarjeta de Crédito")]
        TarjetaCredito = 4
    }

    public enum EstadoPago
    {
        [Display(Name = "Pendiente")]
        Pendiente = 0,
        [Display(Name = "Pagado")]
        Pagado = 1,
        [Display(Name = "Vencido")]
        Vencido = 2
    }

    public enum TipoIngreso
    {
        [Display(Name = "Socio al Día")]
        SocioAlDia = 1,
        [Display(Name = "Socio Moroso")]
        SocioMoroso = 2,
        [Display(Name = "No Socio")]
        NoSocio = 3,
        [Display(Name = "Invitado / Cortesía")]
        Invitado = 4
    }

    public enum TipoVehiculo
    {
        [Display(Name = "Auto")]
        Auto = 1,
        [Display(Name = "Moto")]
        Moto = 2,
        [Display(Name = "Camioneta")]
        Camioneta = 3,
        [Display(Name = "Micro / Combi")]
        Micro = 4
    }
}
