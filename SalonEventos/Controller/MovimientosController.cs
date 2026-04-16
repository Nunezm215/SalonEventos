using Microsoft.AspNetCore.Mvc;
using SalonEventos.Data;
using SalonEventos.Models;
using System;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


namespace SalonEventos.Controllers
{
    public class MovimientosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MovimientosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔥 GET
        public IActionResult Create(int eventoId)
        {
            var movimiento = new Movimiento
            {
                EventoId = eventoId,
                Fecha = DateTime.Today,
                Tipo = "Ingreso"
            };

            return View(movimiento);
        }

        // 🔥 POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Movimiento movimiento)
        {
            // 🔥 asegurar valores
            if (movimiento.Fecha == default)
                movimiento.Fecha = DateTime.Today;

            if (movimiento.Monto <= 0)
                return RedirectToAction("Index", "Eventos");

            var evento = _context.Eventos.FirstOrDefault(e => e.Id == movimiento.EventoId);

            if (evento == null)
            {
                return Content("ERROR: Evento no encontrado");
            }

            // 🔥 guardar movimiento
            _context.Movimientos.Add(movimiento);

            // 🔥 actualizar evento
            if (movimiento.Tipo == "Ingreso")
                evento.Senia += movimiento.Monto;

            if (movimiento.Tipo == "Gasto")
                evento.MontoTotal += movimiento.Monto;

            evento.SaldoPendiente = evento.MontoTotal - evento.Senia;

            if (evento.SaldoPendiente <= 0)
                evento.Estado = "Pagado";
            else if (evento.Senia > 0)
                evento.Estado = "Señado";

            _context.SaveChanges();

            return RedirectToAction("Index", "Eventos");
        }
       
public IActionResult Comprobante(int id)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var movimiento = _context.Movimientos
                .Where(m => m.Id == id)
                .Select(m => new
                {
                    m.Id,
                    m.Tipo,
                    m.Monto,
                    m.Fecha,
                    m.Descripcion,
                    Cliente = m.Evento.NombreCliente
                })
                .FirstOrDefault();

            if (movimiento == null) return NotFound();

            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logo2.png");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Content().Column(col =>
                    {
                        // 🔥 HEADER
                        col.Item().Row(row =>
                        {
                            if (System.IO.File.Exists(logoPath))
                            {

                            row.ConstantItem(100)
                                .Image(logoPath);
                            }

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("EL GALPON MULTIEVENTOS")
                                    .Bold().FontSize(18);

                                c.Item().Text("Francisco Paula de Castañeda N°660")
                                    .FontSize(10);

                                c.Item().Text("Comprobante de movimiento")
                                    .FontSize(12).Italic();
                            });
                        });

                        col.Item().PaddingVertical(10).LineHorizontal(1);

                        // 🔥 TÍTULO
                        col.Item().AlignCenter().Text("COMPROBANTE")
                            .FontSize(20).Bold();

                        col.Item().PaddingVertical(10);

                        // 🔥 CAJA DE DATOS
                        col.Item().Border(1).Padding(10).Column(c =>
                        {
                            c.Item().Text($"Cliente: {movimiento.Cliente}");
                            c.Item().Text($"Tipo: {movimiento.Tipo}");
                            c.Item().Text($"Fecha: {movimiento.Fecha:dd/MM/yyyy}");

                            if (!string.IsNullOrEmpty(movimiento.Descripcion))
                                c.Item().Text($"Detalle: {movimiento.Descripcion}");
                        });

                        col.Item().PaddingTop(15);

                        // 🔥 MONTO DESTACADO
                        col.Item().AlignCenter().Text($"$ {movimiento.Monto}")
                            .FontSize(24)
                            .Bold();

                        col.Item().PaddingTop(20);

                        col.Item().AlignCenter().Text(
                            "Gracias por confiar en El Galpón Multieventos"
                        ).Italic().FontSize(12);
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);

            return File(stream.ToArray(), "application/pdf", $"Movimiento_{movimiento.Id}.pdf");
        }
    }
}
