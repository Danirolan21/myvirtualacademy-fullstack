-- Migration 002: Replace legacy Notificaciones table with new schema
DROP TABLE IF EXISTS Notificaciones;

CREATE TABLE Notificaciones (
    ID_Notificacion INT          NOT NULL IDENTITY(1,1) PRIMARY KEY,
    ID_Usuario      INT          NOT NULL REFERENCES Usuarios(ID_Usuario),
    Tipo            NVARCHAR(50) NOT NULL,
    Titulo          NVARCHAR(200) NOT NULL,
    Mensaje         NVARCHAR(500) NOT NULL,
    Leida           BIT          NOT NULL DEFAULT 0,
    Fecha_Creacion  DATETIME2    NOT NULL DEFAULT GETUTCDATE(),
    Enviado_Por     INT          NULL     REFERENCES Usuarios(ID_Usuario)
);

CREATE INDEX IX_Notificaciones_Usuario ON Notificaciones (ID_Usuario, Leida);
