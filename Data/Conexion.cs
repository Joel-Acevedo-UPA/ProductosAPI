using Microsoft.Data.SqlClient;

namespace ProductosAPI.Data;

// Conexion.cs — punto único para abrir conexiones a SQL Server
public class Conexion
{
    private readonly string _cadena;

    public Conexion(IConfiguration configuracion)
    {
        _cadena = configuracion.GetConnectionString("ProductosDB")
            ?? throw new InvalidOperationException("Falta la cadena de conexión 'ProductosDB' en appsettings.json");
    }

    // Devuelve una conexión ya abierta; quien la usa la cierra con 'using'
    public SqlConnection Abrir()
    {
        var conexion = new SqlConnection(_cadena);
        conexion.Open();
        return conexion;
    }

    // Crea la base de datos y la tabla si todavía no existen (se llama al arrancar la API)
    public void Inicializar()
    {
        var builder = new SqlConnectionStringBuilder(_cadena);
        var nombreBD = builder.InitialCatalog;

        // 1. Conectarse a 'master' para poder crear la base de datos
        builder.InitialCatalog = "master";
        using (var conexionMaster = new SqlConnection(builder.ConnectionString))
        {
            conexionMaster.Open();
            using var comando = new SqlCommand(
                $"IF DB_ID(N'{nombreBD}') IS NULL CREATE DATABASE [{nombreBD}];", conexionMaster);
            comando.ExecuteNonQuery();
        }

        // 2. Crear la tabla y cargar datos de ejemplo solo si está vacía
        using var conexion = Abrir();
        using var crearTabla = new SqlCommand(@"
            IF OBJECT_ID(N'dbo.Productos', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Productos (
                    Id            INT IDENTITY(1,1) PRIMARY KEY,
                    Nombre        NVARCHAR(100)  NOT NULL,
                    Descripcion   NVARCHAR(500)  NOT NULL DEFAULT '',
                    Categoria     NVARCHAR(50)   NOT NULL DEFAULT '',
                    Precio        DECIMAL(10,2)  NOT NULL,
                    Stock         INT            NOT NULL DEFAULT 0,
                    FechaRegistro DATETIME2      NOT NULL DEFAULT SYSDATETIME()
                );

                INSERT INTO dbo.Productos (Nombre, Descripcion, Categoria, Precio, Stock) VALUES
                    (N'Teclado mecánico', N'Switches rojos, retroiluminado', N'Periféricos', 899.00, 15),
                    (N'Mouse inalámbrico', N'2.4 GHz, 1600 DPI',             N'Periféricos', 349.50, 40),
                    (N'Monitor 24""',      N'Full HD, 75 Hz',                 N'Pantallas',  2499.00, 8);
            END", conexion);
        crearTabla.ExecuteNonQuery();
    }
}
