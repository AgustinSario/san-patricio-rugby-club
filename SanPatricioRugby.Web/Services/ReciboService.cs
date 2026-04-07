using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SanPatricioRugby.DAL.Models;

namespace SanPatricioRugby.Web.Services
{
    public class ReciboService : IReciboService
    {
        // Datos fijos del club
        private const string NombreClub = "SAN PATRICIO RUGBY CLUB";
        private const string RazonSocial = "SAN PATRICIO RUGBY CLUB";
        private const string Domicilio = "Santa Fe 634 - Corrientes, Corrientes";
        private const string Cuit = "30-68793724-0";
        private const string IngresosBrutos = "30687937240";
        private const string InicioActividades = "28/02/1991";
        private const string CondicionIva = "IVA Sujeto Exento";

        public byte[] GenerarReciboPdf(Cuota cuota, Socio socio, ConfiguracionEmail? config = null)
        {
            // Fallback values if config is null or values are missing
            var nombreClub = config?.NombreClub ?? "SAN PATRICIO RUGBY CLUB";
            var razonSocial = config?.RazonSocial ?? "SAN PATRICIO RUGBY CLUB";
            var domicilio = config?.Domicilio ?? "Santa Fe 634 - Corrientes, Corrientes";
            var cuit = config?.Cuit ?? "30-68793724-0";
            var iibb = config?.IngresosBrutos ?? "30687937240";
            var inicioAct = config?.InicioActividades ?? "28/02/1991";
            var condicionIva = config?.CondicionIva ?? "IVA Sujeto Exento";

            var nombreMes = ObtenerNombreMes(cuota.Mes);
            var fechaEmision = cuota.FechaPago ?? DateTime.Now;
            var nroComprobante = cuota.Id.ToString("D8"); // Usar el Id de la cuota
            var periodoDesde = new DateTime(cuota.Anio, cuota.Mes, 1);
            var periodoHasta = periodoDesde.AddMonths(1).AddDays(-1);
            var concepto = $"CUOTA DEL MES DE {nombreMes.ToUpper()} DEL CORRIENTE AÑO DEL SOCIO ,{socio.ApellidoNombre.ToUpper()} -.";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

                    page.Content().Column(col =>
                    {
                        // ─── ENCABEZADO ─────────────────────────────────────────────────────
                        col.Item().Border(1).BorderColor(Colors.Black).Row(headerRow =>
                        {
                            // Bloque izquierdo: Datos del club
                            headerRow.RelativeItem(3).Padding(10).Column(left =>
                            {
                                left.Item().Text(nombreClub).Bold().FontSize(12);
                                left.Item().PaddingTop(5).Text(txt =>
                                {
                                    txt.Span("Razón Social: ").Bold();
                                    txt.Span(razonSocial);
                                });
                                left.Item().PaddingTop(3).Text(txt =>
                                {
                                    txt.Span("Domicilio Comercial: ").Bold();
                                    txt.Span(domicilio);
                                });
                                left.Item().PaddingTop(3).Text(txt =>
                                {
                                    txt.Span("Condición frente al IVA: ").Bold();
                                    txt.Span(condicionIva);
                                });
                            });

                            // Bloque central: C / COD.15
                            headerRow.ConstantItem(65).Border(1).BorderColor(Colors.Black).AlignMiddle().AlignCenter().Column(center =>
                            {
                                center.Item().AlignCenter().Text("C").Bold().FontSize(32);
                                center.Item().AlignCenter().Text("COD. 15").FontSize(8).FontColor(Colors.Grey.Medium);
                            });

                            // Bloque derecho: RECIBO + datos
                            headerRow.RelativeItem(3).Padding(10).Column(right =>
                            {
                                right.Item().Text("RECIBO").Bold().FontSize(20);
                                right.Item().PaddingTop(4).Text(txt =>
                                {
                                    txt.Span("Punto de Venta:  00001   ");
                                    txt.Span("Comp. Nro:  ");
                                    txt.Span(nroComprobante).Bold();
                                });
                                right.Item().PaddingTop(2).Text(txt =>
                                {
                                    txt.Span("Fecha de Emisión:  ");
                                    txt.Span(fechaEmision.ToString("dd/MM/yyyy")).Bold();
                                });
                                right.Item().PaddingTop(6).Text(txt =>
                                {
                                    txt.Span("CUIT:  ");
                                    txt.Span(cuit);
                                });
                                right.Item().PaddingTop(2).Text(txt =>
                                {
                                    txt.Span("Ingresos Brutos:  ");
                                    txt.Span(iibb);
                                });
                                right.Item().PaddingTop(2).Text(txt =>
                                {
                                    txt.Span("Fecha de Inicio de Actividades:  ");
                                    txt.Span(inicioAct);
                                });
                            });
                        });

                        // ─── BANDA: PERIODO ─────────────────────────────────────────────────
                        col.Item().Border(1).BorderColor(Colors.Black).Padding(6).Row(periodoRow =>
                        {
                            periodoRow.RelativeItem().Text(txt =>
                            {
                                txt.Span("Período Facturado Desde:  ").Bold();
                                txt.Span(periodoDesde.ToString("dd/MM/yyyy"));
                                txt.Span("    Hasta:  ").Bold();
                                txt.Span(periodoHasta.ToString("dd/MM/yyyy"));
                            });
                            periodoRow.RelativeItem().AlignRight().Text(txt =>
                            {
                                txt.Span("Fecha de Vto. para el pago:  ").Bold();
                                txt.Span(cuota.FechaVencimiento.ToString("dd/MM/yyyy"));
                            });
                        });

                        // ─── DATOS DEL SOCIO ────────────────────────────────────────────────
                        col.Item().Border(1).BorderColor(Colors.Black).Padding(6).Column(socioCol =>
                        {
                            socioCol.Item().Row(row =>
                            {
                                row.RelativeItem().Text(txt =>
                                {
                                    txt.Span("DNI  ").Bold();
                                    txt.Span(socio.Dni ?? "---");
                                });
                                row.RelativeItem(3).Text(txt =>
                                {
                                    txt.Span("Apellido y Nombre / Razón Social:  ").Bold();
                                    txt.Span(socio.ApellidoNombre.ToUpper());
                                });
                            });
                            socioCol.Item().PaddingTop(4).Row(row =>
                            {
                                row.RelativeItem().Text(txt =>
                                {
                                    txt.Span("Condición frente al IVA:  ").Bold();
                                    txt.Span("Consumidor Final");
                                });
                                row.RelativeItem().Text(txt =>
                                {
                                    txt.Span("Domicilio Comercial:  ").Bold();
                                });
                            });
                            socioCol.Item().PaddingTop(4).Text(txt =>
                            {
                                txt.Span("Condición de venta:  ").Bold();
                                txt.Span("Contado");
                            });
                        });

                        // ─── CUERPO: CONCEPTO ───────────────────────────────────────────────
                        col.Item().PaddingTop(20).Column(bodyCol =>
                        {
                            bodyCol.Item().Text(txt =>
                            {
                                txt.Span("Recibi(mos) la suma de: ").FontSize(11);
                                txt.Span($"$ {cuota.Monto:N2}").Bold().FontSize(12);
                            });
                            bodyCol.Item().PaddingTop(5).Text("en concepto de:").FontSize(10);
                            bodyCol.Item().PaddingTop(5).PaddingLeft(10).Text(concepto).FontSize(11).Italic();
                        });

                        // ─── ESPACIADOR FLEXIBLE ────────────────────────────────────────────
                        col.Item().Extend();

                        // ─── TABLA DE TOTALES ───────────────────────────────────────────────
                        col.Item().AlignRight().Width(230).Border(1).BorderColor(Colors.Black).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1);
                            });

                            // Subtotal
                            table.Cell().Padding(4).AlignRight().Text("Subtotal: $");
                            table.Cell().Padding(4).AlignRight().Text($"{cuota.Monto:N2}");

                            // Bonif
                            table.Cell().Padding(4).AlignRight().Text("Bonif: %0    Importe Bonif: $");
                            table.Cell().Padding(4).AlignRight().Text("0,00");

                            // Subtotal con bonif
                            table.Cell().Padding(4).AlignRight().Text("Subtotal c/Bonif.: $");
                            table.Cell().Padding(4).AlignRight().Text($"{cuota.Monto:N2}");

                            // Otros tributos
                            table.Cell().Padding(4).AlignRight().Text("Importe Otros Tributos: $");
                            table.Cell().Padding(4).AlignRight().Text("0,00");

                            // Total (en negrita)
                            table.Cell().Padding(4).AlignRight().Text("Importe Total: $").Bold();
                            table.Cell().Padding(4).AlignRight().Text($"{cuota.Monto:N2}").Bold();
                        });
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);
            return stream.ToArray();
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
