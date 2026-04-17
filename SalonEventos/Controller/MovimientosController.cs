using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonEventos.Data;
using SalonEventos.Models;
using System;
using System.Linq;
using System.IO;

// QuestPDF
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
            var evento = _context.Eventos.FirstOrDefault(e => e.Id == eventoId);

            if (evento == null) return NotFound();

            ViewBag.MontoTotal = evento.MontoTotal;
            ViewBag.SeniaActual = evento.Senia;

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
            var evento = _context.Eventos.FirstOrDefault(e => e.Id == movimiento.EventoId);

            if (evento == null)
                return NotFound();

            if (movimiento.Fecha == default)
                movimiento.Fecha = DateTime.Today;

            // 🔥 VALIDACIONES

            // Monto válido
            if (movimiento.Monto <= 0)
            {
                ModelState.AddModelError("Monto", "Debe ingresar un monto válido.");
            }

            // 🔥 DESCRIPCIÓN OBLIGATORIA
            if (string.IsNullOrWhiteSpace(movimiento.Descripcion))
            {
                ModelState.AddModelError("Descripcion", "Debe ingresar un detalle del movimiento.");
            }

            // 🔥 NO PAGAR DE MÁS
            if (movimiento.Tipo == "Ingreso")
            {
                var seniaNueva = evento.Senia + movimiento.Monto;

                if (seniaNueva > evento.MontoTotal)
                {
                    ModelState.AddModelError("Monto", "El ingreso supera el monto total del evento.");
                }
            }

            // 🔥 SI HAY ERRORES → VOLVER AL FORM
            if (!ModelState.IsValid)
            {
                ViewBag.MontoTotal = evento.MontoTotal;
                ViewBag.SeniaActual = evento.Senia;

                return View(movimiento);
            }

            // 🔥 GUARDAR
            _context.Movimientos.Add(movimiento);

            // 🔥 ACTUALIZAR EVENTO
            if (movimiento.Tipo == "Ingreso")
                evento.Senia += movimiento.Monto;

            if (movimiento.Tipo == "Gasto")
                evento.MontoTotal += movimiento.Monto;

            evento.MontoRestante = evento.MontoTotal - evento.Senia;

            if (evento.MontoRestante <= 0)
                evento.Estado = "Pagado";
            else if (evento.Senia > 0)
                evento.Estado = "Señado";

            _context.SaveChanges();

            return RedirectToAction("Index", "Eventos");
        }

        // 🔥 PDF
        public IActionResult Comprobante(int id)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var movimiento = _context.Movimientos
                .Include(m => m.Evento)
                .FirstOrDefault(m => m.Id == id);

            if (movimiento == null) return NotFound();

            var evento = movimiento.Evento;
            var movimientos = _context.Movimientos
                .Where(m => m.EventoId == movimiento.EventoId)
                .OrderBy(m => m.Fecha)
                .ThenBy(m => m.Id)
                .ToList();

            decimal seniaAcumulada = evento.Senia;

            var historial = movimientos.Select(m =>
            {
                if (m.Tipo == "Ingreso")
                    seniaAcumulada -= m.Monto;

                var montoRestante = evento.MontoTotal - seniaAcumulada;

                return new
                {
                    Fecha = m.Fecha,
                    Tipo = m.Tipo,
                    Monto = m.Monto,
                    Total = evento.MontoTotal,
                    Senia = seniaAcumulada,
                    Saldo = montoRestante,
                    Detalle = m.Descripcion
                };
            }).ToList();

            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logo2.png");

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    page.Content().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            if (System.IO.File.Exists(logoPath))
                                row.ConstantItem(100).Image(logoPath);

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("EL GALPON MULTIEVENTOS").Bold().FontSize(18);
                                c.Item().Text("Comprobante de movimiento").FontSize(12).Italic();
                            });
                        });

                        col.Item().PaddingVertical(10).LineHorizontal(1);

                        col.Item().AlignCenter().Text("COMPROBANTE").FontSize(20).Bold();

                        col.Item().PaddingVertical(10);

                        col.Item().Border(1).Padding(10).Column(c =>
                        {
                            c.Item().Text($"Cliente: {movimiento.Evento?.NombreCliente}");
                            c.Item().Text($"Tipo: {movimiento.Tipo}");
                            c.Item().Text($"Fecha: {movimiento.Fecha:dd/MM/yyyy}");

                            if (!string.IsNullOrEmpty(movimiento.Descripcion))
                                c.Item().Text($"Detalle: {movimiento.Descripcion}");
                        });

                        col.Item().PaddingTop(15);

                        col.Item().Text("Historial del Evento").Bold().FontSize(14);

                        foreach (var h in historial)
                        {
                            col.Item().Border(1).Padding(10).PaddingBottom(10).Column(c =>
                            {
                                c.Item().Text($"📅 {h.Fecha:dd/MM/yyyy}").Bold();

                                c.Item().Text($"Monto total: ${h.Total}");
                                c.Item().Text($"Seña acumulada: ${h.Senia}");
                                c.Item().Text($"Monto total restante: ${h.Saldo}");

                                if (!string.IsNullOrEmpty(h.Detalle))
                                    c.Item().Text($"Detalle: {h.Detalle}");
                            });
                        }

                        col.Item().PaddingTop(20);

                        var ultimo = historial.LastOrDefault();
                        if (ultimo != null)
                        {
                            col.Item().AlignCenter().Text($"Monto restante final: ${ultimo.Saldo}")
                                .FontSize(16).Bold();
                        }

                        col.Item().AlignCenter()
                            .Text("Gracias por elegirnos ✨")
                            .Italic().FontSize(11);
                    });
                });
            });

            using var stream = new MemoryStream();
            document.GeneratePdf(stream);

            return File(stream.ToArray(), "application/pdf", $"Movimiento_{movimiento.Id}.pdf");
        }
    }
}