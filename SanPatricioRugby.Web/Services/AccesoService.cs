using Microsoft.EntityFrameworkCore;
using SanPatricioRugby.DAL;
using SanPatricioRugby.DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SanPatricioRugby.Web.Services
{
    public class AccesoService : IAccesoService
    {
        private readonly ApplicationDbContext _context;

        public AccesoService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<SocioStatusViewModel> GetSocioStatusAsync(int socioId)
        {
            var socio = await _context.Socios
                .Include(s => s.Cuotas)
                .FirstOrDefaultAsync(s => s.Id == socioId);

            if (socio == null) throw new KeyNotFoundException("Socio no encontrado");

            return BuildStatus(socio);
        }

        public async Task<SocioStatusViewModel?> GetSocioStatusByDniAsync(string dni)
        {
            var socio = await _context.Socios
                .Include(s => s.Cuotas)
                .FirstOrDefaultAsync(s => s.Dni == dni);

            if (socio == null) return null;

            return BuildStatus(socio);
        }

        private SocioStatusViewModel BuildStatus(Socio socio)
        {
            var hoy = DateTime.Today;
            var socioParaCuotas = socio;

            // Si es parte de un grupo familiar y NO es el titular, el estado depende del titular
            if (socio.GrupoFamiliarId.HasValue && !socio.EsTitularGrupoFamiliar)
            {
                var titular = _context.Socios
                    .Include(s => s.Cuotas)
                    .FirstOrDefault(s => s.GrupoFamiliarId == socio.GrupoFamiliarId && s.EsTitularGrupoFamiliar);
                
                if (titular != null)
                {
                    socioParaCuotas = titular;
                }
            }

            var pendientes = socioParaCuotas.Cuotas
                .Where(c => (c.Estado == EstadoPago.Pendiente || c.Estado == EstadoPago.Vencido) 
                            && c.FechaVencimiento <= hoy)
                .OrderBy(c => c.Anio).ThenBy(c => c.Mes)
                .ToList();

            return new SocioStatusViewModel
            {
                Socio = socio,
                AlDia = socio.EsActivo && !pendientes.Any(),
                CuotasPendientes = pendientes,
                TotalDeuda = pendientes.Sum(c => c.Monto)
            };
        }

        public async Task<bool> RegistrarIngresoAsync(RegistroIngreso ingreso)
        {
            _context.Ingresos.Add(ingreso);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> RegistrarEstacionamientoAsync(RegistroEstacionamiento estacionamiento)
        {
            _context.Estacionamientos.Add(estacionamiento);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<ConfiguracionPrecio>> GetPreciosAsync()
        {
            return await _context.Precios.ToListAsync();
        }

        public async Task<decimal> GetPrecioConceptoAsync(string concepto)
        {
            var precio = await _context.Precios.FirstOrDefaultAsync(p => p.Concepto == concepto);
            return precio?.Valor ?? 0;
        }

        public async Task<RecaudacionDiaViewModel> GetRecaudacionDiaAsync(DateTime fecha)
        {
            var inicio = fecha.Date;
            var fin = inicio.AddDays(1);

            var ingresos = await _context.Ingresos
                .Include(i => i.Socio)
                .Where(i => i.Fecha >= inicio && i.Fecha < fin)
                .OrderByDescending(i => i.Fecha)
                .ToListAsync();

            var estacionamientos = await _context.Estacionamientos
                .Where(e => e.Fecha >= inicio && e.Fecha < fin)
                .OrderByDescending(e => e.Fecha)
                .ToListAsync();

            return new RecaudacionDiaViewModel
            {
                Fecha = inicio,
                TotalIngresos = ingresos.Sum(i => i.MontoPagado),
                TotalEstacionamiento = estacionamientos.Sum(e => e.MontoPagado),
                CantidadIngresos = ingresos.Count,
                CantidadVehiculos = estacionamientos.Count,
                
                DetalleIngresos = ingresos,
                DetalleEstacionamiento = estacionamientos,
                
                SociosAlDia = ingresos.Count(i => i.Tipo == TipoIngreso.SocioAlDia),
                SociosMorosos = ingresos.Count(i => i.Tipo == TipoIngreso.SocioMoroso),
                NoSocios = ingresos.Count(i => i.Tipo == TipoIngreso.NoSocio)
            };
        }
    }
}
