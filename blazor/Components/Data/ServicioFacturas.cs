using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace facturas.Components.Data
{
    public class ServicioFacturas
    {
        private string RutaDb => Path.Combine(AppContext.BaseDirectory, "facturas.db");

        public async Task<List<Facturas>> ObtenerFacturas()
        {
            var lista = new List<Facturas>();
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();

            var cmd = cx.CreateCommand();
            cmd.CommandText = "SELECT id, fecha, cliente FROM facturas ORDER BY id DESC";

            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                var f = new Facturas
                {
                    Id = rd.GetInt32(0),
                    Fecha = DateTime.Parse(rd.GetString(1)),
                    Cliente = rd.GetString(2)
                };
                f.Viajes = await ObtenerViajes(f.Id);
                lista.Add(f);
            }
            return lista;
        }

        public async Task<Facturas> ObtenerFacturaPorId(int id)
        {
            Facturas f = null;
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();

            var cmd = cx.CreateCommand();
            cmd.CommandText = "SELECT id, fecha, cliente FROM facturas WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", id);

            using var rd = await cmd.ExecuteReaderAsync();
            if (await rd.ReadAsync())
            {
                f = new Facturas
                {
                    Id = rd.GetInt32(0),
                    Fecha = DateTime.Parse(rd.GetString(1)),
                    Cliente = rd.GetString(2)
                };
                f.Viajes = await ObtenerViajes(f.Id);
            }
            return f;
        }

        private async Task<List<Viaje>> ObtenerViajes(int facturaId)
        {
            var lista = new List<Viaje>();
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();

            var cmd = cx.CreateCommand();
            
            cmd.CommandText = "SELECT id, descripcion, folio, monto, tipo FROM viajes WHERE facturaId = $id";
            cmd.Parameters.AddWithValue("$id", facturaId);

            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                lista.Add(new Viaje
                {
                    Id = rd.GetInt32(0),
                    FacturaId = facturaId,
                    Descripcion = rd.GetString(1),
                    Folio = rd.IsDBNull(2) ? "" : rd.GetString(2),
                    Monto = (decimal)rd.GetDouble(3),
                    Tipo = rd.IsDBNull(4) ? "Local" : rd.GetString(4) 
                });
            }
            return lista;
        }

        public async Task AgregarFactura(Facturas f)
        {
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();

            var cmd = cx.CreateCommand();
            cmd.CommandText = "INSERT INTO facturas(fecha, cliente) VALUES($fecha, $cliente); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$fecha", f.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$cliente", f.Cliente);

            object result = await cmd.ExecuteScalarAsync();

            if (result != null && result != DBNull.Value)
            {
                f.Id = (int)(long)result;
            }

            foreach (var v in f.Viajes)
            {
                await AgregarViaje(f.Id, v);
            }
        }

       
        public async Task ActualizarFactura(Facturas f)
        {
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();

            
            var cmdUpdate = cx.CreateCommand();
            cmdUpdate.CommandText = "UPDATE facturas SET fecha = $fecha, cliente = $cliente WHERE id = $id";
            cmdUpdate.Parameters.AddWithValue("$fecha", f.Fecha.ToString("yyyy-MM-dd"));
            cmdUpdate.Parameters.AddWithValue("$cliente", f.Cliente);
            cmdUpdate.Parameters.AddWithValue("$id", f.Id);
            await cmdUpdate.ExecuteNonQueryAsync();

            
            var cmdDel = cx.CreateCommand();
            cmdDel.CommandText = "DELETE FROM viajes WHERE facturaId = $id";
            cmdDel.Parameters.AddWithValue("$id", f.Id);
            await cmdDel.ExecuteNonQueryAsync();

           
            foreach (var v in f.Viajes)
            {
                await AgregarViaje(f.Id, v);
            }
        }

        private async Task AgregarViaje(int facturaId, Viaje v)
        {
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();

            var cmd = cx.CreateCommand();
           
            cmd.CommandText = "INSERT INTO viajes(facturaId, descripcion, folio, monto, tipo) VALUES($facturaId, $descripcion, $folio, $monto, $tipo)";
            cmd.Parameters.AddWithValue("$facturaId", facturaId);
            cmd.Parameters.AddWithValue("$descripcion", v.Descripcion);
            cmd.Parameters.AddWithValue("$folio", v.Folio);
            cmd.Parameters.AddWithValue("$monto", v.Monto);
            cmd.Parameters.AddWithValue("$tipo", v.Tipo); 

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task EliminarFactura(Facturas f)
        {
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();

            var cmd1 = cx.CreateCommand();
            cmd1.CommandText = "DELETE FROM viajes WHERE facturaId = $id";
            cmd1.Parameters.AddWithValue("$id", f.Id);
            await cmd1.ExecuteNonQueryAsync();

            var cmd2 = cx.CreateCommand();
            cmd2.CommandText = "DELETE FROM facturas WHERE id = $id";
            cmd2.Parameters.AddWithValue("$id", f.Id);
            await cmd2.ExecuteNonQueryAsync();
        }
    }
}