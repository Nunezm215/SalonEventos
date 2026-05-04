using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SalonEventos.Data;
using SalonEventos.Models;
using System.Globalization;

namespace SalonEventos.Controllers
{
    public class ReportesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ReportesController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public IActionResult Index()
        {
            var mesActual = DateTime.Now.Month;
            var anioActual = DateTime.Now.Year;

            var resumen = CalcularResumenMensual(mesActual, anioActual);

            var modelo = new BalanceMensualViewModel
            {
                Mes = mesActual,
                Anio = anioActual,
                Ingresos = resumen.Ingresos,
                PorCobrar = resumen.PorCobrar,
                Gastos = resumen.Gastos,
                Balance = resumen.Balance
            };

            var reporteActual = _context.ReportesMensuales
                .FirstOrDefault(r => r.Mes == mesActual && r.Anio == anioActual);

            ViewBag.ReporteActualId = reporteActual?.Id;
            ViewBag.ReporteActualFecha = reporteActual?.FechaGeneracion;

            return View(modelo);
        }

        public IActionResult Historial()
        {
            var reportes = _context.ReportesMensuales
                .OrderByDescending(r => r.Anio)
                .ThenByDescending(r => r.Mes)
                .ToList();

            return View(reportes);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Generar(int mes, int anio, string? origen)
        {
            if (mes < 1 || mes > 12 || anio < 2000)
            {
                TempData["ReporteError"] = "Período inválido para generar el reporte.";
                if (string.Equals(origen, "historial", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(Historial));
                }

                return RedirectToAction(nameof(Index));
            }

            var resumen = CalcularResumenMensual(mes, anio);
            var eventosMes = _context.Eventos
                .Where(e => e.Fecha.Month == mes && e.Fecha.Year == anio)
                .OrderBy(e => e.Fecha)
                .ToList();

            var reporte = _context.ReportesMensuales.FirstOrDefault(r => r.Mes == mes && r.Anio == anio);

            if (reporte == null)
            {
                reporte = new ReporteMensual
                {
                    Mes = mes,
                    Anio = anio
                };
                _context.ReportesMensuales.Add(reporte);
            }

            reporte.Ingresos = resumen.Ingresos;
            reporte.PorCobrar = resumen.PorCobrar;
            reporte.Gastos = resumen.Gastos;
            reporte.Balance = resumen.Balance;
            reporte.FechaGeneracion = DateTime.Now;

            var directorioReportes = Path.Combine(_environment.ContentRootPath, "Reportes");
            Directory.CreateDirectory(directorioReportes);

            var nombreArchivo = $"Reporte-{anio}-{mes:00}.pdf";
            var rutaCompleta = Path.Combine(directorioReportes, nombreArchivo);

            GenerarPdf(rutaCompleta, reporte, eventosMes);
            reporte.ArchivoPdf = nombreArchivo;

            _context.SaveChanges();

            TempData["ReporteOk"] = $"Reporte mensual {mes:00}/{anio} generado correctamente.";
            if (string.Equals(origen, "historial", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Historial));
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Descargar(int id)
        {
            var reporte = _context.ReportesMensuales.FirstOrDefault(r => r.Id == id);
            if (reporte == null || string.IsNullOrWhiteSpace(reporte.ArchivoPdf))
            {
                return NotFound();
            }

            var rutaCompleta = Path.Combine(_environment.ContentRootPath, "Reportes", reporte.ArchivoPdf);
            if (!System.IO.File.Exists(rutaCompleta))
            {
                return NotFound();
            }

            return PhysicalFile(rutaCompleta, "application/pdf", reporte.ArchivoPdf);
        }

        private (decimal Ingresos, decimal PorCobrar, decimal Gastos, decimal Balance) CalcularResumenMensual(int mes, int anio)
        {
            var eventos = _context.Eventos
                .Where(e => e.Fecha.Month == mes && e.Fecha.Year == anio)
                .ToList();

            decimal ingresos = 0;
            decimal porCobrar = 0;

            foreach (var evento in eventos)
            {
                var estado = NormalizarEstado(evento.Estado);

                if (estado == "Pagado")
                {
                    ingresos += evento.MontoTotal;
                }
                else if (estado == "Señado")
                {
                    ingresos += evento.Senia;
                    porCobrar += evento.MontoRestante;
                }
                else
                {
                    porCobrar += evento.MontoTotal;
                }
            }

            var gastos = _context.Movimientos
                .Where(m => m.Tipo == "Gasto" && m.Fecha.Month == mes && m.Fecha.Year == anio)
                .ToList();

            var totalGastos = gastos.Any() ? gastos.Sum(g => g.Monto) : 0;
            var balance = ingresos - totalGastos;

            return (ingresos, porCobrar, totalGastos, balance);
        }

        private static string NormalizarEstado(string? estado)
        {
            if (estado == "Pagado" || estado == "Señado" || estado == "Reservado")
            {
                return estado;
            }

            return "Reservado";
        }

        private static void GenerarPdf(string rutaCompleta, ReporteMensual reporte, List<Evento> eventosMes)
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var cultura = new CultureInfo("es-AR");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);

                    page.Header()
                        .Column(column =>
                        {
                            column.Item().Text("Salon Eventos").FontSize(18).Bold();
                            column.Item().Text($"Reporte mensual: {reporte.Mes:00}/{reporte.Anio}").FontSize(14);
                            column.Item().Text($"Generado: {reporte.FechaGeneracion:dd/MM/yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken2);
                        });

                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        column.Spacing(14);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(8).Text("Concepto").Bold();
                                header.Cell().Background(Colors.Grey.Lighten2).Padding(8).AlignRight().Text("Monto").Bold();
                            });

                            table.Cell().Padding(8).Text("Ingresos");
                            table.Cell().Padding(8).AlignRight().Text($"{reporte.Ingresos:C}");

                            table.Cell().Padding(8).Text("Gastos");
                            table.Cell().Padding(8).AlignRight().Text($"{reporte.Gastos:C}");

                            table.Cell().Padding(8).Text("Por cobrar");
                            table.Cell().Padding(8).AlignRight().Text($"{reporte.PorCobrar:C}");

                            table.Cell().Background(Colors.Blue.Lighten4).Padding(8).Text("Balance").Bold();
                            table.Cell().Background(Colors.Blue.Lighten4).Padding(8).AlignRight().Text($"{reporte.Balance:C}").Bold();
                        });

                        column.Item().Text($"Lista de eventos (Total: {eventosMes.Count})").FontSize(12).Bold();

                        if (eventosMes.Count == 0)
                        {
                            column.Item().Text("No hubo eventos registrados en este mes.").FontColor(Colors.Grey.Darken2);
                        }
                        else
                        {
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn(1);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(8).Text("Cliente").Bold();
                                    header.Cell().Background(Colors.Grey.Lighten2).Padding(8).Text("Dia").Bold();
                                });

                                foreach (var evento in eventosMes)
                                {
                                    table.Cell().Padding(8).Text(evento.NombreCliente);
                                    table.Cell().Padding(8).Text($"{evento.Fecha.ToString("dddd dd/MM/yyyy", cultura)} {evento.Hora:hh\\:mm}");
                                }
                            });
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text("Reporte generado automáticamente por el sistema.")
                        .FontSize(9)
                        .FontColor(Colors.Grey.Darken1);
                });
            }).GeneratePdf(rutaCompleta);
        }
    }
}

