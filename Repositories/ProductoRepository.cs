using Microsoft.Data.SqlClient;
using ProductosAPI.Data;
using ProductosAPI.Models;

namespace ProductosAPI.Repositories;

// ProductoRepository.cs — el único que habla con la base de datos (SqlConnection + SqlCommand)
public class ProductoRepository : IProductoRepository
{
    private readonly string cadena = Conexion.Cadena;

    // Consulta: todos los productos
    public List<Producto> ObtenerTodos()
    {
        using var con = new SqlConnection(cadena);
        con.Open();
        var cmd = new SqlCommand("SELECT * FROM Productos ORDER BY Id", con);
        using var lector = cmd.ExecuteReader();

        var productos = new List<Producto>();
        while (lector.Read())
            productos.Add(LeerProducto(lector));
        return productos;
    }

    // Consulta: un producto por su Id
    public Producto? ObtenerPorId(int id)
    {
        using var con = new SqlConnection(cadena);
        con.Open();
        var cmd = new SqlCommand("SELECT * FROM Productos WHERE Id = @id", con);
        cmd.Parameters.AddWithValue("@id", id);
        using var lector = cmd.ExecuteReader();
        if (!lector.Read()) return null;
        return LeerProducto(lector);
    }

    // Insert: regresa el Id que generó la base de datos
    public int Insertar(Producto producto)
    {
        using var con = new SqlConnection(cadena);
        con.Open();
        var cmd = new SqlCommand(
            "INSERT INTO Productos (Nombre, Descripcion, Categoria, Precio, Stock) " +
            "OUTPUT INSERTED.Id " +
            "VALUES (@nombre, @descripcion, @categoria, @precio, @stock)", con);
        cmd.Parameters.AddWithValue("@nombre",      producto.Nombre);
        cmd.Parameters.AddWithValue("@descripcion", producto.Descripcion);
        cmd.Parameters.AddWithValue("@categoria",   producto.Categoria);
        cmd.Parameters.AddWithValue("@precio",      producto.Precio);
        cmd.Parameters.AddWithValue("@stock",       producto.Stock);
        return (int)cmd.ExecuteScalar();
    }

    // Actualizar: true si encontró el producto y lo cambió
    public bool Actualizar(Producto producto)
    {
        using var con = new SqlConnection(cadena);
        con.Open();
        var cmd = new SqlCommand(
            "UPDATE Productos SET Nombre = @nombre, Descripcion = @descripcion, " +
            "Categoria = @categoria, Precio = @precio, Stock = @stock " +
            "WHERE Id = @id", con);
        cmd.Parameters.AddWithValue("@nombre",      producto.Nombre);
        cmd.Parameters.AddWithValue("@descripcion", producto.Descripcion);
        cmd.Parameters.AddWithValue("@categoria",   producto.Categoria);
        cmd.Parameters.AddWithValue("@precio",      producto.Precio);
        cmd.Parameters.AddWithValue("@stock",       producto.Stock);
        cmd.Parameters.AddWithValue("@id",          producto.Id);
        return cmd.ExecuteNonQuery() > 0;
    }

    // Eliminar: true si el producto existía
    public bool Eliminar(int id)
    {
        using var con = new SqlConnection(cadena);
        con.Open();
        var cmd = new SqlCommand("DELETE FROM Productos WHERE Id = @id", con);
        cmd.Parameters.AddWithValue("@id", id);
        return cmd.ExecuteNonQuery() > 0;
    }

    // Buscar: productos cuyo nombre o categoría contengan el texto
    public List<Producto> Buscar(string texto)
    {
        using var con = new SqlConnection(cadena);
        con.Open();
        var cmd = new SqlCommand(
            "SELECT * FROM Productos WHERE Nombre LIKE @texto OR Categoria LIKE @texto ORDER BY Nombre", con);
        cmd.Parameters.AddWithValue("@texto", "%" + texto + "%");
        using var lector = cmd.ExecuteReader();

        var productos = new List<Producto>();
        while (lector.Read())
            productos.Add(LeerProducto(lector));
        return productos;
    }

    // Arma un Producto con la fila en la que está el lector
    private static Producto LeerProducto(SqlDataReader lector)
    {
        return new Producto
        {
            Id            = (int)lector["Id"],
            Nombre        = (string)lector["Nombre"],
            Descripcion   = (string)lector["Descripcion"],
            Categoria     = (string)lector["Categoria"],
            Precio        = (decimal)lector["Precio"],
            Stock         = (int)lector["Stock"],
            FechaRegistro = (DateTime)lector["FechaRegistro"]
        };
    }
}
