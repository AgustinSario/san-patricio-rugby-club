using SanPatricioRugby.DAL.Models;

namespace SanPatricioRugby.Web.Services
{
    public interface IEmailService
    {
        Task<bool> EnviarReciboAsync(Cuota cuota, Socio socio, byte[] pdfRecibo, string emailDestino);
        Task<bool> HayConfiguracionAsync();
    }
}
