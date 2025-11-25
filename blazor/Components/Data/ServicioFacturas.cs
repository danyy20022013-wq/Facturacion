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
            return await ObtenerFacturasQuery("SELECT id, fecha, cliente, archivada FROM facturas WHERE archivada = 0 ORDER BY id DESC");
        }

       
        public async Task<List<Facturas>> ObtenerFacturasArchivadas()
        {
            return await ObtenerFacturasQuery("SELECT id, fecha, cliente, archivada FROM facturas WHERE archivada = 1 ORDER BY id DESC");
        }

        private async Task<List<Facturas>> ObtenerFacturasQuery(string query)
        {
            var lista = new List<Facturas>();
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();
            var cmd = cx.CreateCommand();
            cmd.CommandText = query;
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                var f = new Facturas
                {
                    Id = rd.GetInt32(0),
                    Fecha = DateTime.Parse(rd.GetString(1)),
                    Cliente = rd.GetString(2),
                    Archivada = rd.GetBoolean(3) 
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
            cmd.CommandText = "SELECT id, fecha, cliente, archivada FROM facturas WHERE id = $id";
            cmd.Parameters.AddWithValue("$id", id);
            using var rd = await cmd.ExecuteReaderAsync();
            if (await rd.ReadAsync())
            {
                f = new Facturas
                {
                    Id = rd.GetInt32(0),
                    Fecha = DateTime.Parse(rd.GetString(1)),
                    Cliente = rd.GetString(2),
                    Archivada = rd.GetBoolean(3)
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
            cmd.CommandText = "INSERT INTO facturas(fecha, cliente, archivada) VALUES($fecha, $cliente, 0); SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$fecha", f.Fecha.ToString("yyyy-MM-dd"));
            cmd.Parameters.AddWithValue("$cliente", f.Cliente);
            object result = await cmd.ExecuteScalarAsync();
            if (result != null && result != DBNull.Value) f.Id = (int)(long)result;
            foreach (var v in f.Viajes) await AgregarViaje(f.Id, v);
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

            foreach (var v in f.Viajes) await AgregarViaje(f.Id, v);
        }

        
        public async Task AlternarArchivo(int id, bool archivar)
        {
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();
            var cmd = cx.CreateCommand();
            cmd.CommandText = "UPDATE facturas SET archivada = $estado WHERE id = $id";
            cmd.Parameters.AddWithValue("$estado", archivar ? 1 : 0);
            cmd.Parameters.AddWithValue("$id", id);
            await cmd.ExecuteNonQueryAsync();
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


        public async Task<List<ReporteDato>> ObtenerIngresosPorTipo()
        {
            return await EjecutarConsultaReporte(@"
                SELECT v.tipo, SUM(v.monto) 
                FROM viajes v 
                JOIN facturas f ON v.facturaId = f.id 
                WHERE f.archivada = 0 
                GROUP BY v.tipo ORDER BY SUM(v.monto) DESC");
        }

        public async Task<List<ReporteDato>> ObtenerMejoresClientes()
        {
            return await EjecutarConsultaReporte(@"
                SELECT f.cliente, SUM(v.monto) 
                FROM facturas f JOIN viajes v ON f.id = v.facturaId 
                WHERE f.archivada = 0
                GROUP BY f.cliente ORDER BY SUM(v.monto) DESC LIMIT 5");
        }

        public async Task<List<ReporteDato>> ObtenerVentasPorMes()
        {
            return await EjecutarConsultaReporte(@"
                SELECT strftime('%Y-%m', f.fecha), SUM(v.monto) 
                FROM facturas f JOIN viajes v ON f.id = v.facturaId 
                WHERE f.archivada = 0
                GROUP BY strftime('%Y-%m', f.fecha) ORDER BY 1 DESC");
        }

        public async Task<List<ReporteDato>> ObtenerVolumenPorTipo()
        {
            return await EjecutarConsultaReporte(@"
                SELECT v.tipo, COUNT(*) 
                FROM viajes v 
                JOIN facturas f ON v.facturaId = f.id 
                WHERE f.archivada = 0
                GROUP BY v.tipo ORDER BY COUNT(*) DESC");
        }

        public async Task<decimal> ObtenerTicketPromedio()
        {
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();
            var cmd = cx.CreateCommand();
            cmd.CommandText = @"
                SELECT AVG(TotalFactura) FROM (
                    SELECT SUM(v.monto) as TotalFactura 
                    FROM viajes v 
                    JOIN facturas f ON v.facturaId = f.id 
                    WHERE f.archivada = 0 
                    GROUP BY v.facturaId
                )";
            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToDecimal(result) : 0;
        }

        private async Task<List<ReporteDato>> EjecutarConsultaReporte(string query)
        {
            var lista = new List<ReporteDato>();
            using var cx = new SqliteConnection($"Data Source={RutaDb}");
            await cx.OpenAsync();
            var cmd = cx.CreateCommand();
            cmd.CommandText = query;
            using var rd = await cmd.ExecuteReaderAsync();
            while (await rd.ReadAsync())
            {
                lista.Add(new ReporteDato { Etiqueta = rd.GetString(0), Valor = rd.GetDecimal(1) });
            }
            return lista;
        }
    }
    public class ReporteDato { public string Etiqueta { get; set; } public decimal Valor { get; set; } }
}