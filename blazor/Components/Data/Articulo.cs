namespace factura.Components.Data
{
    public class Articulo
    {
        public int Id { get; set; }
        public int FacturaId { get; set; }
        public string Nombre { get; set; } = "";
        public int Cantidad { get; set; } = 1;
        public decimal Precio { get; set; } = 0;

        public decimal Subtotal => Cantidad * Precio;
    }
}