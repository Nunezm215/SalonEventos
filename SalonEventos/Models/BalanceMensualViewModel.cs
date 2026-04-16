namespace SalonEventos.Models
{
    public class BalanceMensualViewModel
    {
        public int Mes { get; set; }
        public int Anio { get; set; }
        public decimal Ingresos { get; set; }
        public decimal PorCobrar { get; set; }
        public decimal Gastos { get; set; }
        public decimal Balance { get; set; }
        public List<ReporteMensual> ReportesRecientes { get; set; } = new();
    }
}
