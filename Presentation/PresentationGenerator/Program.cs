using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;

// Set License
QuestPDF.Settings.License = LicenseType.Community;

var outputPath = @"C:\Users\Agustin\.gemini\antigravity\scratch\SanPatricioRugby\Presentacion_Sistema_SanPatricio.pdf";

// Screenshot Paths
var brainPath = @"C:\Users\Agustin\.gemini\antigravity\brain\2f7921eb-411e-4dab-836a-774eb27e99d4";
var logoPath = @"C:\Users\Agustin\.gemini\antigravity\scratch\SanPatricioRugby\SanPatricioRugby.Web\wwwroot\images\escudo_oficial.png";

var dashboardImg = Path.Combine(brainPath, "dashboard_final_1774972646125.png");
var sociosImg = Path.Combine(brainPath, "socios_final_1774972657085.png");
var morososImg = Path.Combine(brainPath, "morosos_final_1774972681131.png");
var scannerImg = Path.Combine(brainPath, "scanner_final_1774972691565.png");
var carnetImg = Path.Combine(brainPath, "carnet_final_1774972726873.png");

Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4.Landscape());
        page.Margin(1, Unit.Centimetre);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Arial"));

        // 1. Cover Page
        page.Content().Column(col =>
        {
            col.Item().AlignCenter().Height(200).Image(logoPath, ImageScaling.FitHeight);
            col.Item().PaddingTop(2, Unit.Centimetre).AlignCenter().Text("San Patricio Rugby Club").FontSize(48).Bold().FontColor("#B71C1C");
            col.Item().AlignCenter().Text("Sistema de Gestión Integral").FontSize(28).FontColor(Colors.Grey.Darken2);
            col.Item().PaddingTop(5, Unit.Centimetre).AlignCenter().Text("Presentación de Funcionalidades").FontSize(18).Italic();
        });
    });

    // 2. Dashboard
    AddFeaturePage(container, "1. Dashboard Inteligente", 
        "Visualización en tiempo real de socios activos, al día y morosos. Acceso directo a reportes críticos para la toma de decisiones financieras.",
        dashboardImg);

    // 3. Gestión de Socios
    AddFeaturePage(container, "2. Gestión de Socios 360°", 
        "Búsqueda avanzada, filtrado por categorías (Activos, Inactivos, Becados) y acceso a detalles históricos completos.",
        sociosImg);

    // 4. Control de Cuotas
    AddFeaturePage(container, "3. Control de Cuotas", 
        "Seguimiento automatizado de pagos, listado de socios morosos con detalle de meses adeudados y gestión de cobros integrada.",
        morososImg);

    // 5. Módulo de Control de Acceso
    AddFeaturePage(container, "4. Módulo de Control de Acceso", 
        "Interfaz optimizada para operadores de entrada, con validación de carnets mediante escaneo de código de barras 1D (Code 128) y búsqueda manual instantánea por DNI.",
        scannerImg);

    // 6. Carnet Digital Premium
    AddFeaturePage(container, "5. Carnet Digital Premium", 
        "Nuestra última gran implementación. Cada socio cuenta con una credencial digital generada automáticamente que incluye:\n" +
        "• Diseño Institucional: Escudo oficial centrado, paleta de colores de marca y estética de papel rasgado.\n" +
        "• Tecnología de Identificación: Código de barras dinámico basado en el DNI, listo para ser escaneado en el club.\n" +
        "• Portabilidad: Botón de descarga en formato PDF de alta calidad para que el socio lo lleve en su celular o lo imprima.",
        carnetImg);

    // 7. Conclusion
    container.Page(page =>
    {
        page.Size(PageSizes.A4.Landscape());
        page.Margin(1, Unit.Centimetre);
        page.Content().Column(col =>
        {
            col.Item().PaddingTop(10, Unit.Centimetre).AlignCenter().Text("El sistema se muestra robusto, profesional y listo para elevar el estándar administrativo del club.").FontSize(24).Bold().FontColor("#B71C1C");
            col.Item().PaddingTop(2, Unit.Centimetre).AlignCenter().Text("Gracias por su atención.").FontSize(18);
        });
    });

})
.GeneratePdf(outputPath);

Console.WriteLine("PDF generado exitosamente en: " + outputPath);

void AddFeaturePage(IDocumentContainer container, string title, string description, string imagePath)
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4.Landscape());
        page.Margin(1, Unit.Centimetre);
        
        page.Header().Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text(title).FontSize(24).Bold().FontColor("#B71C1C");
                col.Item().PaddingTop(5).Text(description).FontSize(14);
            });
            row.ConstantItem(60).Image(logoPath);
        });

        page.Content().PaddingTop(10).AlignCenter().Image(imagePath, ImageScaling.FitArea);

        page.Footer().AlignCenter().Text(x =>
        {
            x.CurrentPageNumber();
            x.Span(" / ");
            x.TotalPages();
        });
    });
}
