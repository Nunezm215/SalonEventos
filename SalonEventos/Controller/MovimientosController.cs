
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SalonEventos.Data;
using SalonEventos.Models;
using Microsoft.EntityFrameworkCore;

namespace SalonEventos.Controllers
{
    public class MovimientosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MovimientosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // LISTA
        public IActionResult Index()
        {
            var lista = _context.Movimientos
                .Include(m => m.Evento)
                .ToList();

            return View(lista);
        }

        // CREATE GET
        public IActionResult Create(int? eventoId, string? tipo)
        {
            CargarEventos();

            var movimiento = new Movimiento
            {
                EventoId = eventoId ?? 0,
                Tipo = string.Equals(tipo, "Gasto", StringComparison.OrdinalIgnoreCase) ? "Gasto" : "Ingreso",
                Fecha = DateTime.Today,
                Monto = 0
            };

            return View(movimiento);
        }

        // CREATE POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Movimiento movimiento, string? motivoGasto)
        {
            // 🔥 VALIDAR EVENTO
            if (movimiento.EventoId == 0)
            {
                ModelState.AddModelError("EventoId", "Debés seleccionar un evento.");
            }

            // 🔥 VALIDAR MONTO
            if (movimiento.Monto <= 0)
            {
                ModelState.AddModelError("Monto", "El monto debe ser mayor a 0.");
            }

            // 🔥 VALIDACIÓN GASTOS
            if (movimiento.Tipo == "Gasto")
            {
                if (motivoGasto == "Mantenimiento")
                {
                    movimiento.Descripcion = "Mantenimiento";
                }
                else if (motivoGasto == "EventoReservado")
                {
                    var evento = _context.Eventos.FirstOrDefault(e => e.Id == movimiento.EventoId);

                    if (evento == null)
                    {
                        ModelState.AddModelError(string.Empty, "Seleccioná un evento válido.");
                    }
                    else
                    {
                        movimiento.Descripcion = $"Gasto del evento: {evento.NombreCliente} ({evento.Fecha:dd/MM/yyyy})";
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Seleccioná un motivo de gasto.");
                }
            }

            // 🔥 SI TODO OK
            if (ModelState.IsValid)
            {
                _context.Movimientos.Add(movimiento);

                // 🔥 ACTUALIZAR EVENTO
                var evento = _context.Eventos.FirstOrDefault(e => e.Id == movimiento.EventoId);

                if (evento != null)
                {
                    if (movimiento.Tipo == "Ingreso")
                    {
                        evento.Senia += movimiento.Monto;
                    }

                    if (movimiento.Tipo == "Gasto")
                    {
                        evento.MontoTotal += movimiento.Monto;
                    }

                    evento.SaldoPendiente = evento.MontoTotal - evento.Senia;

                    if (evento.SaldoPendiente <= 0)
                        evento.Estado = "Pagado";
                    else if (evento.Senia > 0)
                        evento.Estado = "Señado";
                }

                _context.SaveChanges();

                // 🔥 VOLVER AL EVENTO (MEJOR UX)
                return RedirectToAction("Index", "Eventos");
            }

            CargarEventos();
            return View(movimiento);
        }

        // 🔥 CARGAR EVENTOS (TODOS, no solo reservados)
        private void CargarEventos()
        {
            ViewBag.Eventos = _context.Eventos
                .OrderBy(e => e.Fecha)
                .Select(e => new SelectListItem
                {
                    Value = e.Id.ToString(),
                    Text = $"{e.NombreCliente} - {e.Fecha:dd/MM/yyyy} {e.Hora:hh\\:mm}"
                })
                .ToList();
        }
    }
}
