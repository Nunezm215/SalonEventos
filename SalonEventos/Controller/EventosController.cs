using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonEventos.Data;
using SalonEventos.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

// QuestPDF
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SalonEventos.Controllers
{
    public class EventosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public EventosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LISTA
        public async Task<IActionResult> Index()
        {
            var eventos = await _context.Eventos
                .Include(e => e.Movimientos)
                .ToListAsync();

            return View(eventos);
        }

        // CREATE
        public IActionResult Create()
        {
            return View(new Evento
            {
                Fecha = DateTime.Today,
                Hora = new TimeSpan(18, 0, 0),
                TipoEvento = "Cumple infantil",
                Estado = "Reservado",
                CantidadInvitados = 0
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Evento evento)
        {
            // 🔒 NORMALIZAR VALORES
            evento.MontoTotal = Math.Max(0, evento.MontoTotal);
            evento.Senia = Math.Max(0, evento.Senia);

            // 🔥 VALIDACIONES

            // 1. Seña obligatoria
            if (evento.Senia <= 0)
            {
                ModelState.AddModelError("Senia", "Debe ingresar una seña para reservar el evento.");
            }

            // 2. Seña no puede superar el total
            if (evento.Senia > evento.MontoTotal)
            {
                ModelState.AddModelError("Senia", "La seña no puede ser mayor al monto total.");
            }

            // 🔥 VALIDACIÓN DE SUPERPOSICIÓN (3 HORAS)
            var inicio = evento.Hora;
            var fin = evento.Hora.Add(TimeSpan.FromHours(3));

            var eventosMismoDia = _context.Eventos
                .Where(e => e.Fecha.Date == evento.Fecha.Date)
                .ToList();

            var conflicto = eventosMismoDia
                .FirstOrDefault(e =>
                {
                    var inicioExistente = e.Hora;
                    var finExistente = e.Hora.Add(TimeSpan.FromHours(3));

                    return
                        (inicio >= inicioExistente && inicio < finExistente) ||
                        (fin > inicioExistente && fin <= finExistente) ||
                        (inicio <= inicioExistente && fin >= finExistente);
                });

            if (conflicto != null)
            {
                TempData["ErrorEvento"] =
                    $"Horario ocupado por {conflicto.NombreCliente} ({conflicto.Hora:hh\\:mm} - {conflicto.Hora.Add(TimeSpan.FromHours(3)):hh\\:mm})";

                return View(evento);
            }

            // 🔥 SI TODO ESTÁ OK
            if (ModelState.IsValid)
            {
                // 👉 CALCULAR AL FINAL (IMPORTANTE)
                evento.MontoRestante = evento.MontoTotal - evento.Senia;
                evento.Estado = CalcularEstado(evento);

                _context.Eventos.Add(evento);
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            }

            return View(evento);
        }

        // EDIT
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var evento = await _context.Eventos.FindAsync(id);
            if (evento == null) return NotFound();

            return View(evento);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Evento evento)
        {
            if (id != evento.Id) return NotFound();

            var eventoDb = _context.Eventos.Find(id);
            if (eventoDb == null) return NotFound();

            // 🔒 NORMALIZAR
            evento.MontoTotal = Math.Max(0, evento.MontoTotal);
            evento.Senia = Math.Max(0, evento.Senia);

            // 🔥 VALIDACIONES

            // 1. Seña obligatoria
            if (evento.Senia <= 0)
            {
                ModelState.AddModelError("Senia", "Debe ingresar una seña para reservar el evento.");
            }

            // 2. Seña no puede superar el total
            if (evento.Senia > evento.MontoTotal)
            {
                ModelState.AddModelError("Senia", "La seña no puede ser mayor al monto total.");
            }

            // 🔥 VALIDACIÓN DE SUPERPOSICIÓN
            var inicio = evento.Hora;
            var fin = evento.Hora.Add(TimeSpan.FromHours(3));

            var eventosMismoDia = _context.Eventos
                .Where(e => e.Fecha.Date == evento.Fecha.Date && e.Id != evento.Id)
                .ToList();

            var conflicto = eventosMismoDia
                .FirstOrDefault(e =>
                {
                    var inicioExistente = e.Hora;
                    var finExistente = e.Hora.Add(TimeSpan.FromHours(3));

                    return
                        (inicio >= inicioExistente && inicio < finExistente) ||
                        (fin > inicioExistente && fin <= finExistente) ||
                        (inicio <= inicioExistente && fin >= finExistente);
                });

            if (conflicto != null)
            {
                TempData["ErrorEvento"] =
                    $"Horario ocupado por {conflicto.NombreCliente} ({conflicto.Hora:hh\\:mm} - {conflicto.Hora.Add(TimeSpan.FromHours(3)):hh\\:mm})";

                return View(evento);
            }

            // 🔥 SI TODO ESTÁ OK
            if (ModelState.IsValid)
            {
                // 👉 ACTUALIZAR DATOS
                eventoDb.NombreCliente = evento.NombreCliente;
                eventoDb.Telefono = evento.Telefono;
                eventoDb.Fecha = evento.Fecha;
                eventoDb.Hora = evento.Hora;
                eventoDb.TipoEvento = evento.TipoEvento;
                eventoDb.CantidadInvitados = evento.CantidadInvitados;

                // 👉 SOLO SI PERMITÍS EDITAR MONTO Y SEÑA
                eventoDb.MontoTotal = evento.MontoTotal;
                eventoDb.Senia = evento.Senia;

                // 👉 CALCULAR AL FINAL (CLAVE)
                eventoDb.MontoRestante = eventoDb.MontoTotal - eventoDb.Senia;
                eventoDb.Estado = CalcularEstado(eventoDb);

                _context.Update(eventoDb);
                _context.SaveChanges();

                return RedirectToAction(nameof(Index));
            }

            return View(evento);
        }

        // DELETE
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var evento = await _context.Eventos
                .Include(e => e.Movimientos)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (evento != null)
            {
                if (evento.Movimientos != null && evento.Movimientos.Any())
                {
                    _context.Movimientos.RemoveRange(evento.Movimientos);
                }

                _context.Eventos.Remove(evento);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // ESTADO
        private static string CalcularEstado(Evento evento)
        {
            if (evento.MontoRestante <= 0) return "Pagado";
            if (evento.Senia > 0) return "Señado";
            return "Reservado";
        }

        // PDF (igual que tenías)
        public IActionResult DescargarPdf(int id)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var evento = _context.Eventos.FirstOrDefault(e => e.Id == id);
            if (evento == null) return NotFound();

            var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logo.png");   // fondo
            var logo2Path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logo2.png"); // header

            using var stream = new MemoryStream();

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);

                    // 🔥 LOGO DE FONDO (TRANSPARENTE)
                    page.Background().AlignCenter().AlignMiddle().Element(bg =>
                    {
                        if (System.IO.File.Exists(logoPath))
                        {
                            bg.Scale(0.5f).Image(logoPath);
                        }
                    });

                    page.Content().Column(col =>
                    {
                        // 🔥 HEADER CON LOGO
                        col.Item().Row(row =>
                        {
                            if (System.IO.File.Exists(logo2Path))
                            {
                                row.ConstantItem(100).Image(logo2Path);
                            }

                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("EL GALPON MULTIEVENTOS").Bold().FontSize(18);
                                c.Item().Text("Francisco Paula de Castañeda N°660").FontSize(10);
                                c.Item().Text("Resumen del Evento").FontSize(12).Italic();
                            });
                        });

                        col.Item().PaddingVertical(10).LineHorizontal(1);

                        // 🔥 TÍTULO
                        col.Item().AlignCenter().Text("RESUMEN DEL EVENTO")
                            .FontSize(20).Bold();

                        col.Item().PaddingVertical(15);

                        // 🔥 DATOS DEL EVENTO (CAJA)
                        col.Item().Border(1).Padding(15).Column(c =>
                        {
                            c.Item().Text($"Cliente: {evento.NombreCliente}");
                            c.Item().Text($"Fecha: {evento.Fecha:dd/MM/yyyy}");
                            c.Item().Text($"Hora: {evento.Hora}");

                            c.Item().PaddingTop(10);

                            c.Item().Text("💰 Información de pago").Bold();

                            c.Item().Text($"Monto total: ${evento.MontoTotal}");
                            c.Item().Text($"Seña: ${evento.Senia}");
                            c.Item().Text($"Monto pendiente: ${evento.MontoRestante}");
                        });

                        col.Item().PaddingTop(20);

                        // 🔥 ESTADO DESTACADO
                        col.Item().AlignCenter().Text($"Estado: {evento.Estado}")
                            .FontSize(14).Bold();

                        col.Item().PaddingTop(25);

                        col.Item().AlignCenter().Text("Gracias por elegirnos para tu evento ✨")
                            .FontSize(12)
                            .Italic()
                            .FontColor(Colors.Grey.Darken2);
                    });
                });
            }).GeneratePdf(stream);

            return File(stream.ToArray(), "application/pdf", $"Evento_{evento.Id}.pdf");
        }
        public IActionResult GetEventosJson()
        {
            var eventos = _context.Eventos
                .Select(e => new
                {
                    title = e.NombreCliente,
                    start = e.Fecha.ToString("yyyy-MM-dd"),
                    hora = e.Hora.ToString()
                })
                .ToList();

            return Json(eventos);
        }
    }
} 
