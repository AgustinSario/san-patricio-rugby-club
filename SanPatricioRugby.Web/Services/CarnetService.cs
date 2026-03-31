using SkiaSharp;
using SanPatricioRugby.DAL.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;

namespace SanPatricioRugby.Web.Services
{
    public class CarnetService : ICarnetService
    {
        private const int Width = 800;
        private const int Height = 500;
        private const string FontName = "Arial";

        public async Task<string> GenerarCarnetImagenAsync(Socio socio, string rootPath)
        {
            if (string.IsNullOrEmpty(socio.Dni))
                throw new ArgumentException("El DNI es obligatorio para generar el carnet.");

            var fileName = $"carnet_{socio.Id}.png";
            var relativePath = Path.Combine("images", "carnets", fileName);
            var fullPath = Path.Combine(rootPath, "wwwroot", relativePath);

            var dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

            using (var surface = SKSurface.Create(new SKImageInfo(Width, Height)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                // 1. Dibujar fondo rojo superior
                using (var paint = new SKPaint { Color = SKColor.Parse("#B71C1C"), Style = SKPaintStyle.Fill })
                {
                    // Fondo rojo oscuro
                    canvas.DrawRect(0, 0, Width, Height * 0.7f, paint);
                }

                // 2. Dibujar efecto de papel roto (simulado con un path irregular)
                using (var path = new SKPath())
                {
                    path.MoveTo(0, Height * 0.65f);
                    float step = 20;
                    var random = new Random();
                    for (float x = step; x <= Width; x += step)
                    {
                        path.LineTo(x, Height * 0.68f + random.Next(-10, 10));
                    }
                    path.LineTo(Width, Height);
                    path.LineTo(0, Height);
                    path.Close();

                    using (var paint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill })
                    {
                        canvas.DrawPath(path, paint);
                    }
                    
                    // Sombra sutil del papel roto
                    using(var paintLine = new SKPaint { Color = SKColors.LightGray, Style = SKPaintStyle.Stroke, StrokeWidth = 2 })
                    {
                        // canvas.DrawPath(path, paintLine); // Opcional
                    }
                }

                        // 3. Dibujar Escudo (Foto circular con fondo blanco)
                        var escudoPath = Path.Combine(rootPath, "wwwroot", "images", "escudo_oficial.png");
                        if (File.Exists(escudoPath))
                        {
                            using (var stream = File.OpenRead(escudoPath))
                            using (var bitmap = SKBitmap.Decode(stream))
                            {
                                float circleSize = 240;
                                float circleX = 50 + circleSize / 2;
                                float circleY = 45 + circleSize / 2;
                                var brandRed = SKColor.Parse("#B71C1C");

                                // Fondo del círculo en BLANCO
                                using (var backgroundPaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Fill, IsAntialias = true })
                                {
                                    canvas.DrawCircle(circleX, circleY, circleSize / 2, backgroundPaint);
                                }

                                // Centrear la imagen dentro del círculo
                                canvas.Save();
                                using (var circlePath = new SKPath())
                                {
                                    circlePath.AddCircle(circleX, circleY, circleSize / 2);
                                    canvas.ClipPath(circlePath, SKClipOperation.Intersect, true);
                                }
                                
                                float scale = Math.Min(circleSize / bitmap.Width, circleSize / bitmap.Height) * 0.95f; 
                                float imgW = bitmap.Width * scale;
                                float imgH = bitmap.Height * scale;
                                var destRect = new SKRect(
                                    circleX - imgW / 2, 
                                    circleY - imgH / 2, 
                                    circleX + imgW / 2, 
                                    circleY + imgH / 2
                                );
                                
                                using (var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High })
                                {
                                    canvas.DrawBitmap(bitmap, destRect, paint);
                                }
                                canvas.Restore();

                                // Borde del círculo con el mismo rojo externo para integración
                                using (var borderPaint = new SKPaint { Color = brandRed, Style = SKPaintStyle.Stroke, StrokeWidth = 6, IsAntialias = true })
                                {
                                    canvas.DrawCircle(circleX, circleY, circleSize / 2, borderPaint);
                                }
                            }
                        }

                // 4. Dibujar Textos del Club
                using (var paint = new SKPaint { Color = SKColors.White, Typeface = SKTypeface.FromFamilyName(FontName, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), TextSize = 45, IsAntialias = true })
                {
                    canvas.DrawText("SAN PATRICIO", 450, 80, paint);
                    paint.TextSize = 35;
                    paint.Typeface = SKTypeface.FromFamilyName(FontName, SKFontStyleWeight.Light, SKFontStyleWidth.Normal, SKFontStyleSlant.Italic);
                    canvas.DrawText("RUGBY CLUB", 450, 120, paint);
                }

                // 5. Dibujar Datos del Socio
                var (apellido, nombre) = SplitApellidoNombre(socio.ApellidoNombre);

                // APELLIDO
                using (var labelPaint = new SKPaint { Color = SKColors.Black, TextSize = 20, Typeface = SKTypeface.FromFamilyName(FontName, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), IsAntialias = true })
                {
                    canvas.DrawText("APELLIDO", 450, 180, labelPaint);
                    using (var valuePaint = new SKPaint { Color = SKColors.White, TextSize = 40, Typeface = SKTypeface.FromFamilyName(FontName, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), IsAntialias = true })
                    {
                        canvas.DrawText(apellido.ToUpper(), 450, 225, valuePaint);
                    }

                    // NOMBRE
                    canvas.DrawText("NOMBRE", 450, 265, labelPaint);
                    using (var valuePaint = new SKPaint { Color = SKColors.White, TextSize = 40, Typeface = SKTypeface.FromFamilyName(FontName, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), IsAntialias = true })
                    {
                        canvas.DrawText(nombre.ToUpper(), 450, 310, valuePaint);
                    }
                }

                // 6. Dibujar Texto Legal inferior
                using (var legalPaint = new SKPaint { Color = SKColors.Black, TextSize = 16, IsAntialias = true })
                {
                    string legal1 = "Esta credencial es personal e intransferible para uso";
                    string legal2 = "exclusivo del titular. En caso de extravío devolverla a";
                    string legal3 = "San Patricio Rugby Club - cobranzas.sprc@gmail.com";
                    
                    canvas.DrawText(legal1, 80, 420, legalPaint);
                    canvas.DrawText(legal2, 80, 445, legalPaint);
                    canvas.DrawText(legal3, 80, 470, legalPaint);
                }

                // 7. Dibujar Código de Barras
                try 
                {
                    var cleanedDni = socio.Dni?.Replace(".", "").Replace("-", "").Trim() ?? "";
                    // Usamos showLabel: false para evitar duplicación y superposición con nuestro texto manual
                    var barcode = new NetBarcode.Barcode(cleanedDni, NetBarcode.Type.Code128, false);
                    
                    var base64 = barcode.GetBase64Image();
                    byte[] bytes;
                    if (base64.Contains(","))
                    {
                        bytes = Convert.FromBase64String(base64.Split(',')[1]);
                    }
                    else
                    {
                        bytes = Convert.FromBase64String(base64);
                    }
                    
                    using (var ms = new MemoryStream(bytes))
                    using (var barcodeBitmap = SKBitmap.Decode(ms))
                    {
                        // Reposicionar el código de barras (solo las barras, sin texto interno)
                        // Ubicación: Parte inferior derecha, aprovechando el espacio blanco
                        var barcodeRect = new SKRect(Width - 260, 360, Width - 40, 440);
                        canvas.DrawBitmap(barcodeBitmap, barcodeRect);

                        // DNI Texto (Manual, más grande y legible)
                        using (var dniPaint = new SKPaint 
                        { 
                            Color = SKColors.Black, 
                            TextSize = 22, 
                            IsAntialias = true, 
                            TextAlign = SKTextAlign.Center, 
                            Typeface = SKTypeface.FromFamilyName(FontName, SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright) 
                        })
                        {
                            canvas.DrawText(cleanedDni, Width - 150, 475, dniPaint);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generando barcode: {ex.Message}");
                }

                using (var image = surface.Snapshot())
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var stream = File.OpenWrite(fullPath))
                {
                    data.SaveTo(stream);
                }
            }

            return relativePath;
        }

        public async Task<byte[]> GenerarCarnetPdfAsync(Socio socio, string rootPath)
        {
            var relativeImagePath = socio.CarnetPath;
            if (string.IsNullOrEmpty(relativeImagePath))
            {
                relativeImagePath = await GenerarCarnetImagenAsync(socio, rootPath);
            }

            var imagePath = Path.Combine(rootPath, "wwwroot", relativeImagePath);

            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    // Tamaño carnet idéntico al de la imagen para que no se deforme
                    // Convertimos px a puntos (1px = 0.75 puntos aprox, pero QuestPDF maneja unidades mixtas)
                    page.Size(Width * 0.75f, Height * 0.75f, Unit.Point);
                    page.Margin(0);
                    page.Content().Image(imagePath);
                });
            });

            return document.GeneratePdf();
        }

        private (string apellido, string nombre) SplitApellidoNombre(string full)
        {
            if (string.IsNullOrEmpty(full)) return ("", "");
            
            // Intenta separar por coma primero "APELLIDO, Nombre"
            if (full.Contains(","))
            {
                var parts = full.Split(',');
                return (parts[0].Trim(), string.Join(",", parts.Skip(1)).Trim());
            }

            // Si no hay coma, intenta separar por espacios "APELLIDO Nombre1 Nombre2"
            var spaceParts = full.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (spaceParts.Length > 0)
            {
                return (spaceParts[0].Trim(), string.Join(" ", spaceParts.Skip(1)).Trim());
            }

            return (full.Trim(), "");
        }
    }
}
