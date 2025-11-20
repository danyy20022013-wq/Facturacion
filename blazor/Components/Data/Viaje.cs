namespace facturas.Components.Data
{
    public class Viaje
    {
        public int Id { get; set; }
        public int FacturaId { get; set; }
        public string Descripcion { get; set; } = "";


        public string Folio { get; set; } = "";

        public decimal Monto { get; set; } = 0;


        public decimal Subtotal => Monto;
    }
}