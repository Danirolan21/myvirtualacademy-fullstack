-- Migration 003: Ensure IsAdmin column exists and admin user is flagged
-- Idempotent — safe to run multiple times.

IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'IsAdmin'
)
BEGIN
    ALTER TABLE Usuarios
        ADD IsAdmin BIT NOT NULL DEFAULT 0;
END
GO

-- Ensure the first registered admin has IsAdmin = 1
UPDATE Usuarios
SET IsAdmin = 1
WHERE ID_Usuario = 1 AND IsAdmin = 0;
GO
