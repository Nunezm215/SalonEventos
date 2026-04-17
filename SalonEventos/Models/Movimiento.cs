using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalonEventos.Models
{
    public class Movimiento
    {
        public int Id { get; set; }

        public string Tipo { get; set; }

        public decimal Monto { get; set; }

        public DateTime Fecha { get; set; }

        public string? Descripcion { get; set; }

        public int EventoId { get; set; }

       
        public Evento? Evento { get; set; }
    }
}