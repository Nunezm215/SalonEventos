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

        // 🔥 LISTA (CORREGIDO)
        public async Task<IActionResult> Index()
        {
            var eventos = await _context.Eventos
                .Include(e => e.Movimientos) // 👈 CLAVE
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
                    evento.MontoTotal = Math.Max(0, evento.MontoTotal);
                    evento.Senia = Math.Max(0, evento.Senia);

                    if (evento.Senia > evento.MontoTotal)
                        evento.Senia = evento.MontoTotal;

                    evento.SaldoPendiente = evento.MontoTotal - evento.Senia;
                    evento.Estado = CalcularEstado(evento);

                    if (ModelState.IsValid)
                    {
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

                    if (ModelState.IsValid)
                    {
                        eventoDb.NombreCliente = evento.NombreCliente;
                        eventoDb.Telefono = evento.Telefono;
                        eventoDb.Fecha = evento.Fecha;
                        eventoDb.Hora = evento.Hora;
                        eventoDb.TipoEvento = evento.TipoEvento;
                        eventoDb.CantidadInvitados = evento.CantidadInvitados;

                        eventoDb.SaldoPendiente = eventoDb.MontoTotal - eventoDb.Senia;
                        eventoDb.Estado = CalcularEstado(eventoDb);

                        _context.Update(eventoDb);
                        _context.SaveChanges();

                        return RedirectToAction(nameof(Index));
                    }

                    return View(evento);
                }

                // ELIMINAR
                public async Task<IActionResult> Delete(int? id)
                {
                    if (id == null) return NotFound();

                    var evento = await _context.Eventos.FirstOrDefaultAsync(m => m.Id == id);
                    if (evento == null) return NotFound();

                    return View(evento);
                }

                [HttpPost, ActionName("Delete")]
                [ValidateAntiForgeryToken]
                public async Task<IActionResult> DeleteConfirmed(int id)
                {
                    var evento = await _context.Eventos.FindAsync(id);
                    if (evento != null)
                    {
                        _context.Eventos.Remove(evento);
                        await _context.SaveChangesAsync();
                    }

                    return RedirectToAction(nameof(Index));
                }

                // CALCULAR ESTADO
                private static string CalcularEstado(Evento evento)
                {
                    if (evento.SaldoPendiente <= 0) return "Pagado";
                    if (evento.Senia > 0) return "Señado";
                    return "Reservado";
                }

                // 🔥 PDF
                public IActionResult DescargarPdf(int id)
                {
                    QuestPDF.Settings.License = LicenseType.Community;

                    var evento = _context.Eventos.FirstOrDefault(e => e.Id == id);
                    if (evento == null) return NotFound();

                    var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logo.png");
                    var logo2Path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/logo2.png");

                    var document = Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Margin(30);

                            // MARCA DE AGUA
                            page.Background().AlignCenter().AlignMiddle().Element(bg =>
                            {
                                if (System.IO.File.Exists(logoPath))
                                {
                                    bg.Scale(0.5f).Image(logoPath);
                                }
                            });

                            page.Content().Column(col =>
                            {
                                // LOGO IZQUIERDA
                                if (System.IO.File.Exists(logo2Path))
                                {
                                    col.Item()
                                        .AlignLeft()
                                        .Width(120)
                                        .Image(logo2Path);
                                }

                                // TITULO
                                col.Item().Text("EL GALPON MULTIEVENTOS")
                                    .FontSize(24)
                                    .Bold();

                                col.Item().PaddingBottom(5);
                                col.Item().LineHorizontal(1);

                                col.Item().PaddingTop(10);

                                col.Item().Text("Resumen del Evento")
                                    .FontSize(20).Bold();

                                col.Item().PaddingTop(10);

                                col.Item().Text($"Cliente: {evento.NombreCliente}").FontSize(16);
                                col.Item().Text($"Fecha: {evento.Fecha.ToShortDateString()}").FontSize(16);
                                col.Item().Text($"Hora: {evento.Hora}").FontSize(16);
                                col.Item().Text("Dirección: Fransico Paula de Castañeda N°660 entre Paris y V.De la Plaza, Pontevedra, Merlo.")
                                    .FontSize(16);

                                col.Item().PaddingTop(10);

                                col.Item().Text("💰 Información de pago").FontSize(16).Bold();

                                col.Item().Text($"Monto total: ${evento.MontoTotal}").FontSize(16);
                                col.Item().Text($"Seña: ${evento.Senia}").FontSize(16);
                                col.Item().Text($"Saldo pendiente: ${evento.SaldoPendiente}").FontSize(16);

                                col.Item().PaddingTop(20);

                                col.Item().Text(
                                    "Queremos agradecerte sinceramente por confiar en nosotros para un momento tan especial.\n" +
                                    "Es un honor ser parte de tu evento y acompañarte en un día tan importante.\n\n" +
                                    "Vamos a dar lo mejor para que sea una experiencia inolvidable.\n\n" +
                                    "¡Te esperamos con todo listo para disfrutar! 🎉"
                                )
                                .Italic()
                                .FontSize(14);
                            });
                        });
                    });

                    using var stream = new MemoryStream();
                    document.GeneratePdf(stream);

                    return File(stream.ToArray(), "application/pdf", $"Evento_{evento.Id}.pdf");
                }
            }
        }
