using ProductosAPI.Models;
using ProductosAPI.Repositories;

namespace ProductosAPI.Endpoints;

// ProductosEndpoints.cs
public static class ProductosEndpoints
{
    // Este método registra todos los endpoints del grupo
    public static void MapProductoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/productos").WithTags("Productos");

        // GET /productos — 200 con todos los productos
        group.MapGet("/", (IProductoRepository repo) => Results.Ok(repo.ObtenerTodos()));

        // GET /productos/buscar?texto=mouse — 200 con las coincidencias
        group.MapGet("/buscar", (IProductoRepository repo, string texto) =>
            string.IsNullOrWhiteSpace(texto)
                ? Results.BadRequest(new { error = "Escribe un texto para buscar" })
                : Results.Ok(repo.Buscar(texto))
        );

        // GET /productos/{id} — 200 con el producto o 404
        group.MapGet("/{id:int}", (IProductoRepository repo, int id) =>
        {
            var producto = repo.ObtenerPorId(id);
            return producto is null
                ? Results.NotFound(new { mensaje = "Producto no encontrado" })
                : Results.Ok(producto);
        });

        // POST /productos — 201 con el producto creado o 400 si los datos son inválidos
        group.MapPost("/", (IProductoRepository repo, Producto nuevo) =>
        {
            var error = Validar(nuevo);
            if (error != null) return Results.BadRequest(new { error }); //400

            var id = repo.Insertar(nuevo);
            return Results.Created($"/productos/{id}", repo.ObtenerPorId(id)); //201
        });

        // PUT /productos/{id} — 204 si se actualizó, 400 o 404
        group.MapPut("/{id:int}", (IProductoRepository repo, int id, Producto actualizado) =>
        {
            var error = Validar(actualizado);
            if (error != null) return Results.BadRequest(new { error }); //400

            actualizado.Id = id;
            return repo.Actualizar(actualizado)
                ? Results.NoContent() //204
                : Results.NotFound(new { mensaje = "Producto no encontrado" });
        });

        // DELETE /productos/{id} — 204 si se eliminó o 404
        group.MapDelete("/{id:int}", (IProductoRepository repo, int id) =>
            repo.Eliminar(id)
                ? Results.NoContent() //204
                : Results.NotFound(new { mensaje = "Producto no encontrado" })
        );
    }

    // Validación manual antes de tocar la base de datos
    static string? Validar(Producto producto)
    {
        if (string.IsNullOrWhiteSpace(producto.Nombre))
            return "El nombre es obligatorio";

        if (producto.Nombre.Length > 100)
            return "El nombre no puede superar 100 caracteres";

        if (producto.Precio <= 0)
            return "El precio debe ser mayor a 0";

        if (producto.Stock < 0)
            return "El stock no puede ser negativo";

        return null;
    }
}
