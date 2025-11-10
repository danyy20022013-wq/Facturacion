using factura.Components.Data;
using System;
using System.Collections.Generic;

namespace factura.Components.Data
{
    public class Factura
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Cliente { get; set; } = "";
        public List<Articulo> Articulos { get; set; } = new();

        public decimal Total
        {
            get
            {
                decimal suma = 0;
                foreach (var a in Articulos)
                    suma += a.Subtotal;
                return suma;
            }
        }
    }
}