using factura.Components;
using facturas.Components;
using facturas.Components.Data;
using Microsoft.Data.Sqlite;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Registramos el servicio de facturas
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

// Configuración de la Base de Datos
string rutaDb = Path.Combine(AppContext.BaseDirectory, "facturas.db");

using (var cx = new SqliteConnection($"Data Source={rutaDb}"))
{
    cx.Open();

    var cmd = cx.CreateCommand();
    cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS facturas(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            fecha TEXT,
            cliente TEXT
        );

        /* Lógica de Viajes (Fase 3) */
        CREATE TABLE IF NOT EXISTS viajes(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            facturaId INTEGER,
            descripcion TEXT,
            folio TEXT,
            monto REAL DEFAULT 0
        );
    ";
    cmd.ExecuteNonQuery();
}

app.Run();