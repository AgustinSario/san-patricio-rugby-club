using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;
using SanPatricioRugby.DAL;
using SanPatricioRugby.DAL.Models;

namespace SanPatricioRugby.Web.Services
{
    public class EmailService : IEmailService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailService> _logger;

        public EmailService(ApplicationDbContext context, ILogger<EmailService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HayConfiguracionAsync()
        {
            return await _context.ConfiguracionesEmail.AnyAsync();
        }

        public async Task<bool> EnviarReciboAsync(Cuota cuota, Socio socio, byte[] pdfRecibo, string emailDestino)
        {
            var config = await _context.ConfiguracionesEmail.FirstOrDefaultAsync();
            if (config == null)
            {
                _logger.LogWarning("No hay configuración de email guardada. No se envió el recibo.");
                return false;
            }

            try
            {
                var nombreMes = ObtenerNombreMes(cuota.Mes);
                var asunto = $"Recibo de Pago - Cuota {nombreMes} {cuota.Anio} | San Patricio Rugby Club";

                using var mensaje = new MailMessage();
                mensaje.From = new MailAddress(config.EmailRemitente, config.NombreRemitente);
                mensaje.To.Add(new MailAddress(emailDestino));
                mensaje.Subject = asunto;
                mensaje.IsBodyHtml = true;
                mensaje.Body = ConstruirCuerpoEmail(socio, cuota, nombreMes);

                // Adjuntar PDF del recibo
                var adjunto = new Attachment(new MemoryStream(pdfRecibo), 
                    $"Recibo_{socio.ApellidoNombre.Replace(" ", "_")}_{cuota.Mes:00}_{cuota.Anio}.pdf",
                    "application/pdf");
                mensaje.Attachments.Add(adjunto);

                using var smtp = new SmtpClient(config.SmtpHost, config.SmtpPort);
                smtp.Credentials = new NetworkCredential(config.SmtpUser, config.SmtpPassword);
                smtp.EnableSsl = config.UsarSsl;
                smtp.Timeout = 15000;

                await smtp.SendMailAsync(mensaje);
                _logger.LogInformation($"Recibo enviado exitosamente a {emailDestino} para cuota {cuota.Mes}/{cuota.Anio} del socio {socio.ApellidoNombre}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al enviar recibo a {emailDestino}");
                return false;
            }
        }

        private string ConstruirCuerpoEmail(Socio socio, Cuota cuota, string nombreMes)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: Arial, sans-serif; background: #f5f5f5; margin: 0; padding: 0; }}
    .container {{ max-width: 600px; margin: 30px auto; background: white; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
    .header {{ background: #1a1a2e; color: white; padding: 30px; text-align: center; }}
    .header h1 {{ margin: 0; font-size: 22px; letter-spacing: 1px; }}
    .header p {{ margin: 5px 0 0; font-size: 13px; opacity: 0.8; }}
    .badge {{ display: inline-block; background: #e63946; color: white; font-size: 11px; padding: 3px 10px; border-radius: 20px; margin-top: 8px; }}
    .body {{ padding: 30px; }}
    .greeting {{ font-size: 16px; color: #333; margin-bottom: 20px; }}
    .recibo-box {{ background: #f8f9fa; border: 1px solid #e0e0e0; border-radius: 8px; padding: 20px; margin: 20px 0; }}
    .recibo-row {{ display: flex; justify-content: space-between; padding: 8px 0; border-bottom: 1px solid #eee; font-size: 14px; }}
    .recibo-row:last-child {{ border-bottom: none; }}
    .label {{ color: #666; }}
    .value {{ font-weight: bold; color: #222; }}
    .total-row {{ background: #1a1a2e; color: white; border-radius: 6px; padding: 12px 20px; display: flex; justify-content: space-between; margin-top: 15px; font-size: 16px; font-weight: bold; }}
    .footer {{ text-align: center; padding: 20px; background: #f8f9fa; font-size: 12px; color: #888; border-top: 1px solid #eee; }}
    .alert {{ background: #fff3cd; border-left: 4px solid #f0a500; padding: 12px 16px; margin: 15px 0; border-radius: 4px; font-size: 13px; color: #664d03; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>⚪ SAN PATRICIO RUGBY CLUB</h1>
      <p>Comprobante de Pago</p>
      <span class='badge'>RECIBO OFICIAL</span>
    </div>
    <div class='body'>
      <p class='greeting'>Estimado/a <strong>{socio.ApellidoNombre}</strong>,</p>
      <p style='color:#555; font-size:14px;'>Adjunto encontrará el recibo por el pago registrado. A continuación el resumen:</p>
      
      <div class='recibo-box'>
        <div class='recibo-row'>
          <span class='label'>Período</span>
          <span class='value'>{nombreMes.ToUpper()} {cuota.Anio}</span>
        </div>
        <div class='recibo-row'>
          <span class='label'>Fecha de Pago</span>
          <span class='value'>{cuota.FechaPago?.ToString("dd/MM/yyyy") ?? DateTime.Now.ToString("dd/MM/yyyy")}</span>
        </div>
        <div class='recibo-row'>
          <span class='label'>Medio de Pago</span>
          <span class='value'>{cuota.MedioPagoUtilizado}</span>
        </div>
        <div class='recibo-row'>
          <span class='label'>Concepto</span>
          <span class='value'>Cuota Mensual de Socio</span>
        </div>
      </div>
      
      <div class='total-row'>
        <span>IMPORTE TOTAL</span>
        <span>$ {cuota.Monto:N2}</span>
      </div>

      <div class='alert' style='margin-top:20px;'>
        📎 El recibo oficial en formato PDF está adjunto a este correo.
      </div>
    </div>
    <div class='footer'>
      San Patricio Rugby Club · Santa Fe 634, Corrientes<br>
      Este es un mensaje automático, por favor no responda este correo.
    </div>
  </div>
</body>
</html>";
        }

        private static string ObtenerNombreMes(int mes) => mes switch
        {
            1 => "Enero", 2 => "Febrero", 3 => "Marzo", 4 => "Abril",
            5 => "Mayo", 6 => "Junio", 7 => "Julio", 8 => "Agosto",
            9 => "Septiembre", 10 => "Octubre", 11 => "Noviembre", 12 => "Diciembre",
            _ => mes.ToString()
        };
    }
}
