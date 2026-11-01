using ProductosAPI.Data;
using ProductosAPI.Endpoints;
using ProductosAPI.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Inyección de dependencias
builder.Services.AddSingleton<Conexion>();
builder.Services.AddScoped<IProductoRepository, ProductoRepository>();


var app = builder.Build();

// Crear la base de datos y la tabla si no existen
app.Services.GetRequiredService<Conexion>().Inicializar();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

// Registrar todos los endpoints de Productos
app.MapProductoEndpoints();

app.Run();
