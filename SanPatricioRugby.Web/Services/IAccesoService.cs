using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SanPatricioRugby.DAL.Models;

namespace SanPatricioRugby.Web.Services
{
    public interface IAccesoService
    {
        Task<SocioStatusViewModel> GetSocioStatusAsync(int socioId);
        Task<SocioStatusViewModel?> GetSocioStatusByDniAsync(string dni);
        Task<bool> RegistrarIngresoAsync(RegistroIngreso ingreso);
        Task<bool> RegistrarEstacionamientoAsync(RegistroEstacionamiento estacionamiento);
        Task<IEnumerable<ConfiguracionPrecio>> GetPreciosAsync();
        Task<decimal> GetPrecioConceptoAsync(string concepto);
        Task<RecaudacionDiaViewModel> GetRecaudacionDiaAsync(DateTime fecha);
    }

    public class SocioStatusViewModel
    {
        public Socio Socio { get; set; } = null!;
        public bool AlDia { get; set; }
        public List<Cuota> CuotasPendientes { get; set; } = new();
        public decimal TotalDeuda { get; set; }
    }

    public class RecaudacionDiaViewModel
    {
        public DateTime Fecha { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalEstacionamiento { get; set; }
        public int CantidadIngresos { get; set; }
        public int CantidadVehiculos { get; set; }
        
        // Detalle para reporte histórico
        public List<RegistroIngreso> DetalleIngresos { get; set; } = new();
        public List<RegistroEstacionamiento> DetalleEstacionamiento { get; set; } = new();
        
        // Conteo por tipo de ingreso
        public int SociosAlDia { get; set; }
        public int SociosMorosos { get; set; }
        public int NoSocios { get; set; }
    }
}
