using System.ComponentModel.DataAnnotations;

namespace SalonEventos.Models
{
    public class Movimiento
    {
        public int Id { get; set; }

        public string Tipo { get; set; } // Pago, Seña, Gasto, Servicio

        public decimal Monto { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        public string Descripcion { get; set; }

        // 🔥 RELACION CON EVENTO
        public int EventoId { get; set; }
        public Evento Evento { get; set; }
    }
}
