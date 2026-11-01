using System.Text.Json.Serialization;
using Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orders.Api;

var builder = WebApplication.CreateBuilder(args);

// --- Configuración: todo viene de variables de entorno (nada hardcodeado) ---
var connectionString = builder.Configuration.GetConnectionString("Orders")
    ?? throw new InvalidOperationException("Falta la variable ConnectionStrings__Orders");
var rabbitHost = builder.Configuration["RabbitMq:Host"]
    ?? throw new InvalidOperationException("Falta la variable RabbitMq__Host");
var rabbitUser = builder.Configuration["RabbitMq:Username"]
    ?? throw new InvalidOperationException("Falta la variable RabbitMq__Username");
var rabbitPass = builder.Configuration["RabbitMq:Password"]
    ?? throw new InvalidOperationException("Falta la variable RabbitMq__Password");

// --- Servicios ---
builder.Services.AddDbContext<OrdersDbContext>(o => o.UseNpgsql(connectionString));

// El estado se devuelve como texto ("Pending") en el JSON.
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// CORS: permite que el frontend (otro origen) llame a esta API.
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<StockReservedConsumer>();
    x.AddConsumer<StockRejectedConsumer>();

    // Outbox: el evento se guarda en la base en la MISMA transacción que el pedido
    // y un proceso en segundo plano lo envía a RabbitMQ cuando el broker está disponible.
    x.AddEntityFrameworkOutbox<OrdersDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(rabbitHost, "/", h =>
        {
            h.Username(rabbitUser);
            h.Password(rabbitPass);
        });
        cfg.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(2)));
        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();
app.UseCors();

// --- Al arrancar: crea la base de datos y las tablas si no existen, y carga el catálogo ---
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    await db.Database.EnsureCreatedAsync();
    await db.SeedAsync();
}

// --- Endpoints ---
app.MapPost("/orders", async (CrearPedidoRequest request, OrdersDbContext db, IPublishEndpoint bus) =>
{
    var sku = request.Sku?.Trim() ?? "";
    var skuExiste = await db.Productos.AnyAsync(p => p.Sku == sku);

    var errores = PedidoValidador.Validar(request, skuExiste);
    if (errores.Count > 0)
        return Results.ValidationProblem(errores); // 400

    var order = new Order(request.ClienteNombre!.Trim(), sku, request.Cantidad);
    db.Orders.Add(order);

    // Con el Outbox, esta publicación se guarda junto al pedido en el mismo SaveChanges.
    await bus.Publish(new OrderCreated(Guid.NewGuid(), order.Id, order.Sku, order.Cantidad, DateTime.UtcNow));
    await db.SaveChangesAsync();

    return Results.Created($"/orders/{order.Id}", order); // 201
});

app.MapGet("/orders", async (OrdersDbContext db) =>
    await db.Orders.OrderByDescending(o => o.CreadoEn).ToListAsync());

app.MapGet("/orders/{id:guid}", async (Guid id, OrdersDbContext db) =>
    await db.Orders.FindAsync(id) is { } order ? Results.Ok(order) : Results.NotFound());

app.Run();
