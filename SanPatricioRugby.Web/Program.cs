using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using SanPatricioRugby.DAL;
using SanPatricioRugby.DAL.Models;
using SanPatricioRugby.Web.Services;
using QuestPDF.Infrastructure;

// Configurar Licencia de QuestPDF (requerido)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=DESKTOP-BG81C3S;Database=SanPatricioDB;User Id=rck;Password=Sa1457;TrustServerCertificate=True;"));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = false; // Permitir acceso123!
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<ImportService>();
builder.Services.AddScoped<IAccesoService, AccesoService>();
builder.Services.AddScoped<ICarnetService, CarnetService>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Role and Admin User Seeding
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    // context.Database.EnsureCreated();

    var adminRole = "Admin";
    var accessRole = "Control de Acceso";
    if (!await roleManager.RoleExistsAsync(adminRole))
    {
        await roleManager.CreateAsync(new IdentityRole(adminRole));
    }
    if (!await roleManager.RoleExistsAsync(accessRole))
    {
        await roleManager.CreateAsync(new IdentityRole(accessRole));
    }

    var adminEmail = "admin@sanpatricio.com";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var user = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, "Admin123!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, adminRole);
        }
    }

    var accessUser = "acceso1";
    var accessEmail = "acceso1@sanpatricio.com";
    var userAcceso = await userManager.FindByNameAsync(accessUser);
    if (userAcceso == null)
    {
        userAcceso = new IdentityUser { UserName = accessUser, Email = accessEmail, EmailConfirmed = true };
        await userManager.CreateAsync(userAcceso, "acceso123!");
    }
    
    // Forzar rol y contraseña
    if (!await userManager.IsInRoleAsync(userAcceso, accessRole))
    {
        await userManager.AddToRoleAsync(userAcceso, accessRole);
    }
    
    // Asegurar contraseña exacta solicitada (acceso123!)
    var token = await userManager.GeneratePasswordResetTokenAsync(userAcceso);
    await userManager.ResetPasswordAsync(userAcceso, token, "acceso123!");

    // Seed Precios
    if (!context.Precios.Any())
    {
        context.Precios.AddRange(new List<ConfiguracionPrecio>
        {
            new ConfiguracionPrecio { Concepto = "Entrada No Socio", Valor = 2000, Descripcion = "Cobro para quienes no son socios del club" },
            new ConfiguracionPrecio { Concepto = "Entrada Socio Moroso", Valor = 1000, Descripcion = "Cobro reducido para socios que deben cuotas" },
            new ConfiguracionPrecio { Concepto = "Estacionamiento Auto", Valor = 1500 },
            new ConfiguracionPrecio { Concepto = "Estacionamiento Moto", Valor = 500 },
            new ConfiguracionPrecio { Concepto = "Estacionamiento Camioneta", Valor = 2000 }
        });
        await context.SaveChangesAsync();
    }
}

app.Run();
