using factura.Components.Data;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace factura.Components.Data
{
    public class ServicioFacturas
    {
        private string RutaDb => Path.Combine(AppContext.BaseDirectory, "facturas.db");

        public ServicioFacturas()
        {
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            cx.Open();

            var cmd = cx.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS facturas(
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    fecha TEXT,
                    cliente TEXT
                );

                CREATE TABLE IF NOT EXISTS articulos(
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    facturaId INTEGER,
                    nombre TEXT,
                    cantidad INTEGER DEFAULT 1,
                    precio REAL
                );
            ";
            cmd.ExecuteNonQuery();

            var checkCmd = cx.CreateCommand();
            checkCmd.CommandText = "PRAGMA table_info(articulos)";
            using var reader = checkCmd.ExecuteReader();
            bool tieneCantidad = false;
            while (reader.Read())
            {
                if (reader.GetString(1).Equals("cantidad", StringComparison.OrdinalIgnoreCase))
                {
                    tieneCantidad = true;
                    break;
                }
            }
            reader.Close();

            if (!tieneCantidad)
            {
                var alter = cx.CreateCommand();
                alter.CommandText = "ALTER TABLE articulos ADD COLUMN cantidad INTEGER DEFAULT 1";
                alter.ExecuteNonQuery();
            }
        }

        public async Task<List<Factura>> ObtenerFacturas()
        {
            var lista = new List<Factura>();

            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();

            var cmd = cx.CreateCommand();
            cmd.CommandText = "SELECT id, fecha, cliente FROM facturas ORDER BY id DESC";

            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                var f = new Factura
                {
                    Id = rd.GetInt32(0),
                    Fecha = DateTime.Parse(rd.GetString(1)),
                    Cliente = rd.GetString(2)
                };

                f.Articulos = await ObtenerArticulos(f.Id);
                lista.Add(f);
            }

            return lista;
        }

        private async Task<List<Articulo>> ObtenerArticulos(int facturaId)
        {
            var lista = new List<Articulo>();

            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();

            var cmd = cx.CreateCommand();
            cmd.CommandText = "SELECT id, nombre, cantidad, precio FROM articulos WHERE facturaId = $id";
            cmd.Parameters.AddWithValue("$id", facturaId);

            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                lista.Add(new Articulo
                {
                    Id = rd.GetInt32(0),
                    FacturaId = facturaId,
                    Nombre = rd.GetString(1),
                    Cantidad = rd.GetInt32(2),
                    Precio = (decimal)rd.GetDouble(3)
                });
            }

            return lista;
        }

        public async Task AgregarFactura(Factura f)
        {
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();

            var cmd = cx.CreateCommand();
            cmd.CommandText = "INSERT INTO facturas(fecha, cliente) VALUES($fecha, $cliente); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$fecha", f.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$cliente", f.Cliente);

            var id = (long)await cmd.ExecuteScalarAsync();
            f.Id = (int)id;

            foreach (var a in f.Articulos)
            {
                await AgregarArticulo(f.Id, a);
            }
        }

        public async Task<int> AgregarArticulo(int facturaId, Articulo a)
        {
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();

            var cmd = cx.CreateCommand();
            cmd.CommandText = "INSERT INTO articulos(facturaId, nombre, cantidad, precio) VALUES($facturaId, $nombre, $cantidad, $precio); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$facturaId", facturaId);
            cmd.Parameters.AddWithValue("$nombre", a.Nombre);
            cmd.Parameters.AddWithValue("$cantidad", a.Cantidad);
            cmd.Parameters.AddWithValue("$precio", a.Precio);

            var id = (long)await cmd.ExecuteScalarAsync();
            return (int)id;
        }

        public async Task EliminarFactura(Factura f)
        {
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();

            var cmd1 = cx.CreateCommand();
            cmd1.CommandText = "DELETE FROM articulos WHERE facturaId = $id";
            cmd1.Parameters.AddWithValue("$id", f.Id);
            await cmd1.ExecuteNonQueryAsync();

            var cmd2 = cx.CreateCommand();
            cmd2.CommandText = "DELETE FROM facturas WHERE id = $id";
            cmd2.Parameters.AddWithValue("$id", f.Id);
            await cmd2.ExecuteNonQueryAsync();
        }

        public async Task ActualizarFactura(Factura f)
        {
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();

            var cmd = cx.CreateCommand();
            cmd.CommandText = "UPDATE facturas SET fecha = $fecha, cliente = $cliente WHERE id = $id";
            cmd.Parameters.AddWithValue("$fecha", f.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$cliente", f.Cliente);
            cmd.Parameters.AddWithValue("$id", f.Id);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ActualizarArticulo(Articulo a)
        {
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();

            var cmd = cx.CreateCommand();
            cmd.CommandText = "UPDATE articulos SET nombre = $nombre, cantidad = $cantidad, precio = $precio WHERE id = $id";
            cmd.Parameters.AddWithValue("$nombre", a.Nombre);
            cmd.Parameters.AddWithValue("$cantidad", a.Cantidad);
            cmd.Parameters.AddWithValue("$precio", a.Precio);
            cmd.Parameters.AddWithValue("$id", a.Id);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task EliminarArticulo(int articuloId)
        {
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();

            var cmd = cx.CreateCommand();
            cmd.CommandText = "DELETE FROM articulos WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", articuloId);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
namespace factura.Components.Data
{
    public class ServicioFactura
    {
    }
}
