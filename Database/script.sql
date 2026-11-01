-- Ejecutar una vez antes de arrancar la API (SSMS, Azure Data Studio o sqlcmd):
--   sqlcmd -S "(localdb)\MSSQLLocalDB" -f 65001 -i Database/script.sql
IF DB_ID(N'ProductosDB') IS NULL
    CREATE DATABASE ProductosDB;
GO

USE ProductosDB;
GO

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
        (N'Monitor 24"',       N'Full HD, 75 Hz',                 N'Pantallas',  2499.00, 8);
END
GO

-- Consultas equivalentes a cada método del repositorio
-- ObtenerTodos : SELECT * FROM Productos ORDER BY Id
-- ObtenerPorId : SELECT * FROM Productos WHERE Id = @id
-- Insertar     : INSERT INTO Productos (...) OUTPUT INSERTED.Id VALUES (...)
-- Actualizar   : UPDATE Productos SET ... WHERE Id = @id
-- Eliminar     : DELETE FROM Productos WHERE Id = @id
-- Buscar       : SELECT * FROM Productos WHERE Nombre LIKE @texto OR Categoria LIKE @texto
