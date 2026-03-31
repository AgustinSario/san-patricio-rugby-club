using SanPatricioRugby.DAL.Models;

namespace SanPatricioRugby.Web.Services
{
    public interface ICarnetService
    {
        Task<string> GenerarCarnetImagenAsync(Socio socio, string rootPath);
        Task<byte[]> GenerarCarnetPdfAsync(Socio socio, string rootPath);
    }
}
