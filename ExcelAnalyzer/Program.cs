using System;
using System.Linq;
using System.Collections.Generic;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SanPatricioRugby.DAL;
using SanPatricioRugby.DAL.Models;

string excelPath = @"C:\Users\Agustin\.gemini\antigravity\scratch\SanPatricioRugby\sync_temp.xlsx";
string connectionString = "Server=DESKTOP-BG81C3S;Database=SanPatricioDB;User Id=rck;Password=Sa1457;TrustServerCertificate=True;";

var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
optionsBuilder.UseSqlServer(connectionString);

using var context = new ApplicationDbContext(optionsBuilder.Options);
using var workbook = new XLWorkbook(excelPath);

var result = new ImportSummary();

Console.WriteLine("--- Iniciando Depuración y Sincronización Final (Acuerdos + Becas) ---");

// 1. Inactivar socios
var allSocios = context.Socios.ToList();
foreach (var s in allSocios) s.EsActivo = false;
context.SaveChanges();

var dbSocios = allSocios.ToDictionary(s => NormalizeName(s.ApellidoNombre) + "|" + (s.Dni ?? ""), s => s);

// 2. Procesar Lista Maestra (Source of Truth)
var wsMaster = workbook.Worksheets.FirstOrDefault(w => w.Name.Trim() == "LISTADO SOCIOS 2025 JAVIER  (2)");
if (wsMaster != null) {
    Console.WriteLine("Procesando Lista Maestra (2)...");
    ProcessMasterSheet(wsMaster, context, dbSocios, result, true);
}

// 3. Procesar Historial
var wsHistory = workbook.Worksheets.FirstOrDefault(w => w.Name.Trim() == "LISTADO SOCIOS 2025 JAVIER");
if (wsHistory != null) {
    Console.WriteLine("Procesando Historial JAVIER...");
    ProcessMasterSheet(wsHistory, context, dbSocios, result, false);
}

// 4. Débitos
var wsDebitos = workbook.Worksheets.FirstOrDefault(w => w.Name.Trim() == "MONICA DIAZ DEBITOS");
if (wsDebitos != null) {
    Console.WriteLine("Procesando Débitos...");
    ProcessPaymentsSheet(wsDebitos, context, dbSocios, result, MedioPago.Debito);
}

// 5. Transferencias
var wsTransferencias = workbook.Worksheets.FirstOrDefault(w => w.Name.Trim() == "MONICA DIAZ TRASFERENCIAS");
if (wsTransferencias != null) {
    Console.WriteLine("Procesando Transferencias...");
    ProcessPaymentsSheet(wsTransferencias, context, dbSocios, result, MedioPago.Transferencia);
}

context.SaveChanges();
Console.WriteLine("\n--- Sincronización Completada ---");
Console.WriteLine($"Socios: {result.Nuevos} nuevos, {result.Actualizados} actualizados.");
Console.WriteLine($"Cuotas: {result.CuotasCreadas} creadas, {result.CuotasPagadas} pagadas.");

string NormalizeName(string name) => name?.Trim().ToUpper().Replace("  ", " ") ?? "";

void ProcessMasterSheet(IXLWorksheet ws, ApplicationDbContext ctx, Dictionary<string, Socio> dbSocios, ImportSummary summary, bool isActiveList) {
    var rows = ws.RowsUsed().Skip(1);
    foreach (var row in rows) {
        var nombre = row.Cell("J").GetValue<string>()?.Trim();
        if (string.IsNullOrWhiteSpace(nombre) || nombre == "Apellido y Nombre") continue;
        var dni = row.Cell("N").GetValue<string>()?.Trim();
        var key = NormalizeName(nombre) + "|" + (dni ?? "");

        if (!dbSocios.TryGetValue(key, out var socio)) {
            var keyAlt = NormalizeName(nombre) + "|";
            if (!dbSocios.TryGetValue(keyAlt, out socio)) {
                socio = new Socio { ApellidoNombre = nombre, Dni = dni };
                ctx.Socios.Add(socio);
                dbSocios[key] = socio;
                summary.Nuevos++;
            }
        }
        if (isActiveList) {
            socio.EsActivo = true;
            socio.NumeroIdentificador = row.Cell("A").GetValue<string>();
            socio.TipoSocio = row.Cell("B").GetValue<string>();
            socio.Deporte = row.Cell("C").GetValue<string>();
            socio.Division = row.Cell("D").GetValue<string>();
            socio.Camada = row.Cell("E").GetValue<string>();
            socio.Sexo = row.Cell("F").GetValue<string>();
            socio.Celular = row.Cell("G").GetValue<string>();
            socio.NumeroTarjeta = row.Cell("L").GetValue<string>();
            socio.Acuerdos = row.Cell("L").GetValue<string>(); // Mapped to column L
            socio.NombreTitularTarjeta = row.Cell("M").GetValue<string>();
            
            var f2 = row.Cell("I").GetValue<string>();
            if (DateTime.TryParse(f2, out DateTime dt2)) socio.FechaNacimiento2 = dt2;

            socio.MedioPagoPredeterminado = row.Cell("K").GetValue<string>().ToUpper().Contains("DEBITO") ? MedioPago.Debito : MedioPago.Transferencia;
            if (socio.Id > 0) summary.Actualizados++;
        }
        ctx.SaveChanges();

        // Identificar si es BECADO
        bool isBecado = (socio.TipoSocio?.ToUpper().Contains("BECA") ?? false) || 
                         (socio.Acuerdos?.ToUpper().Contains("BECA") ?? false);

        // Cuotas (15 a 28)
        for (int col = 15; col <= 28; col++) {
            var val = row.Cell(col).GetValue<string>()?.Trim().ToUpper();
            int mes = col <= 26 ? col - 14 : col - 26;
            int anio = col <= 26 ? 2025 : 2026;

            // Si es becado y año 2025, forzar estado PAGADO
            if (isBecado && anio == 2025) val = "PAGADO";

            UpdateCuota(ctx, socio, mes, anio, val, socio.MedioPagoPredeterminado, summary);
        }
    }
}

void ProcessPaymentsSheet(IXLWorksheet ws, ApplicationDbContext ctx, Dictionary<string, Socio> dbSocios, ImportSummary summary, MedioPago medio) {
    var rows = ws.RowsUsed().Skip(1);
    int colNombre = (medio == MedioPago.Debito) ? 3 : 1;
    int colDni = (medio == MedioPago.Debito) ? 2 : 3;
    foreach (var row in rows) {
        var nombre = row.Cell(colNombre).GetValue<string>()?.Trim();
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Contains("NOMBRE") || nombre.Contains("TRASFERENCIA")) continue;
        var dni = row.Cell(colDni).GetValue<string>()?.Trim();
        var key = NormalizeName(nombre) + "|" + (dni ?? "");

        if (dbSocios.TryGetValue(key, out var socio)) {
            // Re-chequear si es becado (para no sobreescribir con Pendiente si la hoja de pagos no lo tiene)
            bool isBecado = (socio.TipoSocio?.ToUpper().Contains("BECA") ?? false) || 
                             (socio.Acuerdos?.ToUpper().Contains("BECA") ?? false);

            for (int mes = 1; mes <= 12; mes++) {
                var val = row.Cell(4 + mes - 1).GetValue<string>()?.Trim().ToUpper();
                if (isBecado) val = "PAGADO";
                UpdateCuota(ctx, socio, mes, 2025, val, medio, summary);
            }
        }
    }
}

void UpdateCuota(ApplicationDbContext ctx, Socio socio, int mes, int anio, string val, MedioPago medio, ImportSummary summary) {
    if (string.IsNullOrEmpty(val)) return;
    var cuota = ctx.Cuotas.FirstOrDefault(c => c.SocioId == socio.Id && c.Mes == mes && c.Anio == anio);
    if (cuota == null) {
        cuota = new Cuota { SocioId = socio.Id, Mes = mes, Anio = anio, FechaVencimiento = new DateTime(anio, mes, 10), Monto = 50500 };
        ctx.Cuotas.Add(cuota);
        summary.CuotasCreadas++;
    }
    if (val.Contains("PAGADO") || val.Contains("PAGDO") || val.Contains("PAGADFO") || val.Contains("PAGAGO")) {
        if (cuota.Estado != EstadoPago.Pagado) {
            cuota.Estado = EstadoPago.Pagado;
            cuota.FechaPago = DateTime.Now;
            cuota.MedioPagoUtilizado = medio;
            summary.CuotasPagadas++;
        }
    } else if (decimal.TryParse(val, out decimal monto)) {
        cuota.Monto = monto;
    }
}

class ImportSummary {
    public int Nuevos, Actualizados, CuotasCreadas, CuotasPagadas;
}
