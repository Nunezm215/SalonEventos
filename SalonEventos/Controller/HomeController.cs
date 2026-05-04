using Microsoft.AspNetCore.Mvc;
using SalonEventos.Data;
using SalonEventos.Models;
using Microsoft.EntityFrameworkCore;

namespace SalonEventos.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var hoy = DateTime.Now;

            // Obtener todos los eventos
            var eventos = await _context.Eventos.ToListAsync();

            // 💰 Ingresos del mes
            var ingresosMes = eventos
                .Where(e => e.Fecha.Month == hoy.Month && e.Fecha.Year == hoy.Year)
                .Sum(e => e.MontoTotal);

            // 📅 Eventos del mes
            var eventosMes = eventos
                .Count(e => e.Fecha.Month == hoy.Month && e.Fecha.Year == hoy.Year);

            // 📊 Total eventos
            var totalEventos = eventos.Count;

            // ⚠️ Pendientes (no pagados)
            var pendientes = eventos
                .Count(e => e.Estado != "Pagado");

            // Enviar datos a la vista
            ViewBag.IngresosMes = ingresosMes;
            ViewBag.EventosMes = eventosMes;
            ViewBag.TotalEventos = totalEventos;
            ViewBag.Pendientes = pendientes;

            return View();
        }

        // Redirección para crear evento
        public IActionResult Create()
        {
            return RedirectToAction("Create", "Eventos");
        }
    }
}

