-- Migration 001: Add BCrypt columns and RefreshTokens table
-- Run this script against the MYVIRTUALACADEMY database before deploying the new API.

-- 1. Add BCrypt migration columns to Usuarios
ALTER TABLE Usuarios
    ADD Pass_BCrypt NVARCHAR(255) NULL,
        MigratedToBCrypt BIT NOT NULL DEFAULT 0;
GO

-- 2. Add IsAdmin column (replaces hardcoded IdUsuario == 1 check)
ALTER TABLE Usuarios
    ADD IsAdmin BIT NOT NULL DEFAULT 0;
GO

-- 3. Mark user with ID=1 as admin
UPDATE Usuarios
SET IsAdmin = 1
WHERE ID_Usuario = 1;
GO

-- 4. Create RefreshTokens table
CREATE TABLE RefreshTokens (
    Token       NVARCHAR(512)   NOT NULL PRIMARY KEY,
    IdUsuario   INT             NOT NULL REFERENCES Usuarios(ID_Usuario),
    Expiry      DATETIME2       NOT NULL,
    Revoked     BIT             NOT NULL DEFAULT 0
);
GO

CREATE INDEX IX_RefreshTokens_IdUsuario ON RefreshTokens (IdUsuario);
GO
