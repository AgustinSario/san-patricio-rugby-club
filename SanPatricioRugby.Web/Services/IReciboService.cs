using SanPatricioRugby.DAL.Models;

namespace SanPatricioRugby.Web.Services
{
    public interface IReciboService
    {
        byte[] GenerarReciboPdf(Cuota cuota, Socio socio, ConfiguracionEmail? config = null);
    }
}
