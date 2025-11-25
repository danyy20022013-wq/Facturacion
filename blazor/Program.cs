using factura.Components;
using facturas.Components;
using facturas.Components.Data;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<ServicioFacturas>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

string rutaDb = Path.Combine(AppContext.BaseDirectory, "facturas.db");

using (var cx = new SqliteConnection($"Data Source={rutaDb}"))
{
    cx.Open();

    var cmd = cx.CreateCommand();
    cmd.CommandText = @"
        /* CAMBIO PRINCIPAL: Agregamos 'archivada INTEGER DEFAULT 0' */
        CREATE TABLE IF NOT EXISTS facturas(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            fecha TEXT,
            cliente TEXT,
            archivada INTEGER DEFAULT 0 
        );

        DROP TABLE IF EXISTS articulos; 
        
        CREATE TABLE IF NOT EXISTS viajes(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            facturaId INTEGER,
            tipo TEXT,
            descripcion TEXT,
            folio TEXT,
            monto REAL DEFAULT 0
        );
    ";
    cmd.ExecuteNonQuery();
}

app.Run();