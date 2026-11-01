namespace ProductosAPI.Models;

// Producto.cs — representa una fila de la tabla Productos
public class Producto
{
    public int      Id            { get; set; }
    public string   Nombre        { get; set; } = string.Empty;
    public string   Descripcion   { get; set; } = string.Empty;
    public string   Categoria     { get; set; } = string.Empty;
    public decimal  Precio        { get; set; }
    public int      Stock         { get; set; }
    public DateTime FechaRegistro { get; set; }
}
