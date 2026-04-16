using SalonEventos.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 🔌 Base de datos SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=SalonEventos.db"));

// 🧠 MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ⚠️ Manejo de errores
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// 🔥 NECESARIO PARA CSS / JS / IMÁGENES
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// 🚀 CREAR BASE DE DATOS AUTOMÁTICAMENTE
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
}

// 🧭 RUTAS
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
