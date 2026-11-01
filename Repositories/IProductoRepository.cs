using ProductosAPI.Models;

namespace ProductosAPI.Repositories;

// Interface pedida en la actividad (+ Buscar para el punto "Buscar")
public interface IProductoRepository
{
    List<Producto> ObtenerTodos();
    Producto? ObtenerPorId(int id);
    int Insertar(Producto producto);
    bool Actualizar(Producto producto);
    bool Eliminar(int id);
    List<Producto> Buscar(string texto);
}
