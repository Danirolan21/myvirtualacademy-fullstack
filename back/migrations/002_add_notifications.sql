-- Migration 002: Add Notificaciones table
CREATE TABLE Notificaciones (
    ID_Notificacion INT PRIMARY KEY IDENTITY(1,1),
    ID_Usuario      INT NOT NULL REFERENCES Usuarios(ID_Usuario),
    Tipo            NVARCHAR(50)  NOT NULL,  -- 'entrega', 'calificacion', 'inscripcion'
    Titulo          NVARCHAR(200) NOT NULL,
    Mensaje         NVARCHAR(500) NOT NULL,
    Leida           BIT           NOT NULL DEFAULT 0,
    Fecha_Creacion  DATETIME2     NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Notificaciones_Usuario ON Notificaciones (ID_Usuario, Leida);
