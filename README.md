# ProductosAPI — CRUD de productos con SQL Server

Backend en **ASP.NET Core (Minimal API, .NET 10)** que administra productos guardados en
**SQL Server (LocalDB)**. Implementa la interface `IProductoRepository` con `SqlConnection` y
`SqlCommand` (paquete `Microsoft.Data.SqlClient`) y se prueba desde **Swagger**.

## Cómo ejecutarlo

Requisitos: .NET SDK 10 y SQL Server LocalDB (viene con Visual Studio / SQL Server Express).

1. Crear la base de datos y la tabla con el script (una sola vez). Se puede abrir
   `Database/script.sql` en SSMS y ejecutarlo, o desde la terminal:

   ```bash
   sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i Database/script.sql
   ```

   (`-f 65001` hace que lea el archivo como UTF-8 para que los acentos se guarden bien.)

2. Arrancar la API:

   ```bash
   dotnet run --launch-profile http
   ```

3. Abrir Swagger en `http://localhost:5058/swagger` y probar los endpoints.

Para usar otro servidor de SQL Server basta con cambiar la cadena en `Data/Conexion.cs`.

## Estructura

```
ProductosAPI/
├── Program.cs                      Arranque: servicios, Swagger, CORS y endpoints
├── Models/Producto.cs              Clase Producto (una fila de la tabla)
├── Data/Conexion.cs                Cadena de conexión a SQL Server
├── Repositories/
│   ├── IProductoRepository.cs      Interface pedida en la actividad (+ Buscar)
│   └── ProductoRepository.cs       Implementación con SqlConnection y SqlCommand
├── Endpoints/ProductosEndpoints.cs Rutas HTTP /productos y validación
└── Database/script.sql             Crea la base, la tabla y 3 productos de ejemplo
```

## Endpoints

| Método | Ruta                          | Método del repositorio | Respuesta            |
|--------|-------------------------------|------------------------|----------------------|
| GET    | `/productos`                  | `ObtenerTodos()`       | 200 + lista          |
| GET    | `/productos/{id}`             | `ObtenerPorId(id)`     | 200 o 404            |
| GET    | `/productos/buscar?texto=...` | `Buscar(texto)`        | 200 + coincidencias  |
| POST   | `/productos`                  | `Insertar(producto)`   | 201 o 400            |
| PUT    | `/productos/{id}`             | `Actualizar(producto)` | 204, 400 o 404       |
| DELETE | `/productos/{id}`             | `Eliminar(id)`         | 204 o 404            |

Ejemplo de cuerpo para POST / PUT:

```json
{
  "nombre": "Audífonos USB",
  "descripcion": "Con micrófono",
  "categoria": "Audio",
  "precio": 599.90,
  "stock": 12
}
```

## Flujo del proyecto

La API está dividida en capas, y cada petición pasa por ellas en orden:

```
Cliente (Swagger / app)
   │  HTTP  (GET, POST, PUT, DELETE)
   ▼
Endpoints/ProductosEndpoints.cs   ← recibe la petición, valida y decide el código HTTP
   │  llama a la interface
   ▼
IProductoRepository                ← contrato: qué operaciones existen
   │  implementada por
   ▼
ProductoRepository.cs              ← abre la conexión, arma el SQL con parámetros y lo ejecuta
   │  usa la cadena de
   ▼
Data/Conexion.cs
   │
   ▼
SQL Server → tabla Productos
```

**1. Arranque (`Program.cs`).** Se registran Swagger, CORS y el repositorio en la inyección de
dependencias con `AddSingleton<IProductoRepository, ProductoRepository>()`: cuando un endpoint
pide un `IProductoRepository`, ASP.NET le entrega un `ProductoRepository`. Al final se
registran las rutas con `app.MapProductoEndpoints()` y se arranca con `app.Run()`.

**2. Conexión (`Data/Conexion.cs`).** Guarda la cadena de conexión en un solo lugar
(servidor, base de datos y tipo de autenticación). El repositorio la copia en su campo
`cadena`, así que si cambia el servidor solo se modifica esa línea.

**3. Endpoints (`Endpoints/ProductosEndpoints.cs`).** Agrupa las rutas bajo `/productos`. Cada
ruta recibe el repositorio por inyección de dependencias (nunca crea uno con `new`), así el
endpoint no sabe nada de SQL: solo depende de la interface. Antes de insertar o actualizar,
`Validar()` revisa que el nombre no esté vacío ni pase de 100 caracteres, que el precio sea mayor
a 0 y que el stock no sea negativo; si algo falla responde **400** sin tocar la base de datos.

**4. Repositorio (`Repositories/ProductoRepository.cs`).** Es el único que entra a la base de
datos (como el DAO). Todos sus métodos siguen el mismo patrón:

```csharp
using var con = new SqlConnection(cadena);   // 1. crear la conexión
con.Open();                                  // 2. abrirla
var cmd = new SqlCommand("... WHERE Id = @id", con);   // 3. escribir el SQL
cmd.Parameters.AddWithValue("@id", id);      // 4. pasar los valores como parámetros
// 5. ejecutar (ver abajo); al terminar el método, 'using' cierra la conexión
```

Los valores siempre viajan como **parámetros** (`@id`, `@nombre`, …) y no pegados al texto del
SQL, para evitar inyección SQL. Lo que cambia entre métodos es cómo se ejecuta el comando:

- **Consultas** — `ObtenerTodos()`, `ObtenerPorId()` y `Buscar()` usan `ExecuteReader()`, que
  regresa las filas; se recorren con `lector.Read()` y `LeerProducto()` convierte cada fila en
  un `Producto` leyendo las columnas por nombre (`(int)lector["Id"]`, `(string)lector["Nombre"]`…).
  `Buscar()` usa `LIKE '%texto%'` sobre el nombre y la categoría.
- **Insert** — `Insertar()` usa `ExecuteScalar()`, que regresa un solo valor: el Id que generó
  la columna `IDENTITY`, gracias a `OUTPUT INSERTED.Id`. El endpoint usa ese Id para responder
  **201 Created** con el producto completo.
- **Actualizar** — `Actualizar()` usa `ExecuteNonQuery()`, que regresa cuántas filas cambió;
  si fue 0, el producto no existía y el endpoint responde **404**; si no, **204 No Content**.
- **Eliminar** — `Eliminar()` funciona igual que actualizar, con un `DELETE ... WHERE Id = @id`.

**5. Respuesta.** El endpoint convierte el resultado en una respuesta HTTP (`Results.Ok`,
`Results.Created`, `Results.NoContent`, `Results.NotFound`, `Results.BadRequest`) y ASP.NET
convierte el objeto `Producto` a JSON.

### Ejemplo completo: crear un producto

1. En Swagger se envía `POST /productos` con el JSON del producto.
2. ASP.NET convierte el JSON en un objeto `Producto` y ejecuta el endpoint de `MapPost`.
3. `Validar()` aprueba los datos.
4. Se llama a `repo.Insertar(producto)`: abre la conexión, arma el `INSERT` con parámetros y
   `ExecuteScalar()` regresa el nuevo Id (por ejemplo, 4).
5. El endpoint vuelve a leer el producto con `ObtenerPorId(4)` para incluir la fecha de registro
   que puso SQL Server, y responde `201 Created` con `Location: /productos/4`.

## Pruebas realizadas

Probado con Swagger y `curl` contra LocalDB: listar (200), obtener por Id (200 / 404), insertar
(201), insertar inválido (400), actualizar (204 / 404), buscar por texto (200), eliminar (204) y
eliminar un Id inexistente (404).
