namespace ProductosAPI.Data;

// Conexion.cs — cadena de conexión a SQL Server en un solo lugar
public static class Conexion
{
    // LocalDB viene con Visual Studio / SQL Server Express.
    // Si usas otro servidor, cambia solo esta línea.
    public const string Cadena =
        "Server=(localdb)\\MSSQLLocalDB;Database=ProductosDB;Trusted_Connection=True;TrustServerCertificate=True;";
}
