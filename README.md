# ProductosAPI — CRUD de productos con SQL Server

Backend en **ASP.NET Core (Minimal API, .NET 10)** que administra productos guardados en
**SQL Server (LocalDB)**. Implementa la interface `IProductoRepository` con ADO.NET
(`Microsoft.Data.SqlClient`) y se prueba desde **Swagger**.

## Cómo ejecutarlo

Requisitos: .NET SDK 10 y SQL Server LocalDB (viene con Visual Studio / SQL Server Express).

```bash
dotnet run --launch-profile http
```

Se abre Swagger en `http://localhost:5058/swagger`. La primera vez la API crea sola la base
`ProductosDB`, la tabla `Productos` y tres productos de ejemplo (ver `Database/script.sql`).
Para usar otro servidor basta con cambiar `ConnectionStrings:ProductosDB` en `appsettings.json`.

## Estructura

```
ProductosAPI/
├── Program.cs                      Arranque: servicios, Swagger, CORS y endpoints
├── appsettings.json                Cadena de conexión "ProductosDB"
├── Models/Producto.cs              Clase Producto (una fila de la tabla)
├── Data/Conexion.cs                Abre conexiones y crea la BD/tabla si no existen
├── Repositories/
│   ├── IProductoRepository.cs      Interface pedida en la actividad (+ Buscar)
│   └── ProductoRepository.cs       Implementación con SqlCommand y parámetros
├── Endpoints/ProductosEndpoints.cs Rutas HTTP /productos y validación
└── Database/script.sql             Script SQL equivalente, como referencia
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
ProductoRepository.cs              ← arma el SQL con parámetros y lo ejecuta
   │  pide una conexión
   ▼
Data/Conexion.cs                   ← abre la conexión con la cadena de appsettings.json
   │
   ▼
SQL Server → tabla dbo.Productos
```

**1. Arranque (`Program.cs`).** Se registran los servicios en el contenedor de inyección de
dependencias: `Conexion` como *Singleton* (una sola instancia que guarda la cadena de conexión)
y `IProductoRepository → ProductoRepository` como *Scoped* (una instancia por petición). Antes de
escuchar peticiones se llama a `Conexion.Inicializar()`, que se conecta a `master`, crea la base
`ProductosDB` si no existe y después crea la tabla con datos de ejemplo. Por último se activan
Swagger y CORS y se registran las rutas con `app.MapProductoEndpoints()`.

**2. Conexión (`Data/Conexion.cs`).** Lee `ConnectionStrings:ProductosDB` de `appsettings.json`.
Su método `Abrir()` devuelve una `SqlConnection` ya abierta; el repositorio la usa dentro de un
`using`, así que se cierra (y regresa al pool de conexiones) al terminar cada método.

**3. Endpoints (`Endpoints/ProductosEndpoints.cs`).** Agrupa las rutas bajo `/productos`. Cada
ruta recibe el repositorio por inyección de dependencias (nunca crea uno con `new`), así el
endpoint no sabe nada de SQL: solo depende de la interface. Antes de insertar o actualizar,
`Validar()` revisa que el nombre no esté vacío ni pase de 100 caracteres, que el precio sea mayor
a 0 y que el stock no sea negativo; si algo falla responde **400** sin tocar la base de datos.

**4. Repositorio (`Repositories/ProductoRepository.cs`).** Aquí vive el SQL. Todos los valores
viajan como **parámetros** (`@Id`, `@Nombre`, …) en lugar de concatenarse al texto, lo que evita
inyección SQL. Cada método usa la ejecución de ADO.NET que le corresponde:

- **Consultas** — `ObtenerTodos()`, `ObtenerPorId()` y `Buscar()` usan `ExecuteReader()` y
  recorren las filas con un `SqlDataReader`; el ayudante `Mapear()` convierte cada fila en un
  objeto `Producto`. `Buscar()` usa `LIKE '%texto%'` sobre nombre y categoría.
- **Insert** — `Insertar()` ejecuta un `INSERT ... OUTPUT INSERTED.Id` con `ExecuteScalar()`,
  que regresa el Id que generó la columna `IDENTITY`. El endpoint usa ese Id para responder
  **201 Created** con el producto completo.
- **Actualizar** — `Actualizar()` ejecuta un `UPDATE ... WHERE Id = @Id` con
  `ExecuteNonQuery()`; si el número de filas afectadas es 0, el producto no existía y el endpoint
  responde **404**; si no, **204 No Content**.
- **Eliminar** — `Eliminar()` funciona igual con un `DELETE ... WHERE Id = @Id`.

**5. Respuesta.** El endpoint convierte el resultado en una respuesta HTTP (`Results.Ok`,
`Results.Created`, `Results.NoContent`, `Results.NotFound`, `Results.BadRequest`) y ASP.NET
serializa el objeto `Producto` a JSON.

### Ejemplo completo: crear un producto

1. En Swagger se envía `POST /productos` con el JSON del producto.
2. ASP.NET convierte el JSON en un objeto `Producto` y ejecuta el endpoint de `MapPost`.
3. `Validar()` aprueba los datos.
4. Se llama a `repo.Insertar(producto)`: abre conexión, arma el `INSERT` con parámetros y
   `ExecuteScalar()` regresa el nuevo Id (por ejemplo, 4).
5. El endpoint vuelve a leer el producto con `ObtenerPorId(4)` para incluir la fecha de registro
   que puso SQL Server, y responde `201 Created` con `Location: /productos/4`.

## Pruebas realizadas

Probado con Swagger y `curl` contra LocalDB: listar (200), obtener por Id (200 / 404), insertar
(201), insertar inválido (400), actualizar (204), buscar por texto (200), eliminar (204) y
eliminar un Id inexistente (404).
