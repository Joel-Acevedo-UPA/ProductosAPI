-- Script de referencia: la API ejecuta lo mismo al arrancar (Data/Conexion.cs → Inicializar)
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
-- ObtenerTodos : SELECT * FROM dbo.Productos ORDER BY Id;
-- ObtenerPorId : SELECT * FROM dbo.Productos WHERE Id = @Id;
-- Insertar     : INSERT INTO dbo.Productos (...) OUTPUT INSERTED.Id VALUES (...);
-- Actualizar   : UPDATE dbo.Productos SET ... WHERE Id = @Id;
-- Eliminar     : DELETE FROM dbo.Productos WHERE Id = @Id;
-- Buscar       : SELECT * FROM dbo.Productos WHERE Nombre LIKE @Texto OR Categoria LIKE @Texto;
