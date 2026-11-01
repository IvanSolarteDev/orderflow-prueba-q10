using Inventory.Worker;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

// --- Configuración: todo viene de variables de entorno (nada hardcodeado) ---
var connectionString = builder.Configuration.GetConnectionString("Inventory")
    ?? throw new InvalidOperationException("Falta la variable ConnectionStrings__Inventory");
var rabbitHost = builder.Configuration["RabbitMq:Host"]
    ?? throw new InvalidOperationException("Falta la variable RabbitMq__Host");
var rabbitUser = builder.Configuration["RabbitMq:Username"]
    ?? throw new InvalidOperationException("Falta la variable RabbitMq__Username");
var rabbitPass = builder.Configuration["RabbitMq:Password"]
    ?? throw new InvalidOperationException("Falta la variable RabbitMq__Password");

builder.Services.AddDbContext<InventoryDbContext>(o => o.UseNpgsql(connectionString));
builder.Services.AddScoped<ReservaStock>();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<OrderCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });
        // Reintentos básicos si el procesamiento falla (por ejemplo, la base está momentáneamente caída).
        cfg.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(2)));
        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();

// --- Al arrancar: crea la base de datos y las tablas si no existen, y carga el stock ---
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.SeedAsync();
}

host.Run();
