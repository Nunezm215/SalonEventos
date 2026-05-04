namespace SalonEventos.Models
{
    public class ReporteMensual
    {
        public int Id { get; set; }
        public int Mes { get; set; }
        public int Anio { get; set; }
        public decimal Ingresos { get; set; }
        public decimal Gastos { get; set; }
        public decimal PorCobrar { get; set; }
        public decimal Balance { get; set; }
        public DateTime FechaGeneracion { get; set; }
        public string? ArchivoPdf { get; set; }
    }
}

