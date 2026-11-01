using Microsoft.Data.SqlClient;
using ProductosAPI.Data;
using ProductosAPI.Models;

namespace ProductosAPI.Repositories;

// ProductoRepository.cs — implementa la interface con ADO.NET (SqlCommand + parámetros)
public class ProductoRepository : IProductoRepository
{
    private const string Columnas = "Id, Nombre, Descripcion, Categoria, Precio, Stock, FechaRegistro";

    private readonly Conexion _conexion;

    public ProductoRepository(Conexion conexion)
    {
        _conexion = conexion;
    }

    // Consulta: SELECT de todos los productos
    public List<Producto> ObtenerTodos()
    {
        using var conexion = _conexion.Abrir();
        using var comando = new SqlCommand($"SELECT {Columnas} FROM dbo.Productos ORDER BY Id;", conexion);
        return LeerLista(comando);
    }

    // Consulta: SELECT de un producto por su Id
    public Producto? ObtenerPorId(int id)
    {
        using var conexion = _conexion.Abrir();
        using var comando = new SqlCommand($"SELECT {Columnas} FROM dbo.Productos WHERE Id = @Id;", conexion);
        comando.Parameters.AddWithValue("@Id", id);

        using var lector = comando.ExecuteReader();
        return lector.Read() ? Mapear(lector) : null;
    }

    // Insert: devuelve el Id generado por IDENTITY
    public int Insertar(Producto producto)
    {
        using var conexion = _conexion.Abrir();
        using var comando = new SqlCommand(@"
            INSERT INTO dbo.Productos (Nombre, Descripcion, Categoria, Precio, Stock)
            OUTPUT INSERTED.Id
            VALUES (@Nombre, @Descripcion, @Categoria, @Precio, @Stock);", conexion);
        AgregarParametros(comando, producto);

        return (int)comando.ExecuteScalar();
    }

    // Actualizar: true si encontró y modificó la fila
    public bool Actualizar(Producto producto)
    {
        using var conexion = _conexion.Abrir();
        using var comando = new SqlCommand(@"
            UPDATE dbo.Productos
            SET Nombre = @Nombre, Descripcion = @Descripcion, Categoria = @Categoria,
                Precio = @Precio, Stock = @Stock
            WHERE Id = @Id;", conexion);
        AgregarParametros(comando, producto);
        comando.Parameters.AddWithValue("@Id", producto.Id);

        return comando.ExecuteNonQuery() > 0;
    }

    // Eliminar: true si existía la fila
    public bool Eliminar(int id)
    {
        using var conexion = _conexion.Abrir();
        using var comando = new SqlCommand("DELETE FROM dbo.Productos WHERE Id = @Id;", conexion);
        comando.Parameters.AddWithValue("@Id", id);

        return comando.ExecuteNonQuery() > 0;
    }

    // Buscar: coincidencias parciales en nombre o categoría
    public List<Producto> Buscar(string texto)
    {
        using var conexion = _conexion.Abrir();
        using var comando = new SqlCommand($@"
            SELECT {Columnas} FROM dbo.Productos
            WHERE Nombre LIKE @Texto OR Categoria LIKE @Texto
            ORDER BY Nombre;", conexion);
        comando.Parameters.AddWithValue("@Texto", $"%{texto}%");

        return LeerLista(comando);
    }

    // ---- Ayudantes ----

    private static void AgregarParametros(SqlCommand comando, Producto producto)
    {
        comando.Parameters.AddWithValue("@Nombre",      producto.Nombre);
        comando.Parameters.AddWithValue("@Descripcion", producto.Descripcion);
        comando.Parameters.AddWithValue("@Categoria",   producto.Categoria);
        comando.Parameters.AddWithValue("@Precio",      producto.Precio);
        comando.Parameters.AddWithValue("@Stock",       producto.Stock);
    }

    private static List<Producto> LeerLista(SqlCommand comando)
    {
        var productos = new List<Producto>();
        using var lector = comando.ExecuteReader();
        while (lector.Read())
            productos.Add(Mapear(lector));
        return productos;
    }

    // Convierte la fila actual del lector en un objeto Producto
    private static Producto Mapear(SqlDataReader lector) => new()
    {
        Id            = lector.GetInt32(0),
        Nombre        = lector.GetString(1),
        Descripcion   = lector.GetString(2),
        Categoria     = lector.GetString(3),
        Precio        = lector.GetDecimal(4),
        Stock         = lector.GetInt32(5),
        FechaRegistro = lector.GetDateTime(6),
    };
}
