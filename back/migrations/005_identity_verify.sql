-- =============================================================================
-- 005_identity_verify.sql
-- Verifica que la migración 005 se aplicó correctamente.
-- Ejecutar inmediatamente después de 005_identity_migration.sql, y
-- ANTES de 005_identity_cleanup.sql (necesita las tablas _OLD para CHECK 2).
--
-- Cada check muestra una columna [Estado] con 'OK' o 'FALLO'.
-- Si todos están en OK, la migración es segura para limpiar.
-- =============================================================================

USE [MyVirtualAcademy];
GO

-- =============================================================================
-- CHECK 1: las 8 tablas tienen columna IDENTITY
-- =============================================================================
PRINT '=== CHECK 1: IDENTITY en las 8 tablas afectadas ===';

;WITH expected (Tabla) AS (
    SELECT 'Usuarios' UNION ALL
    SELECT 'Cursos' UNION ALL
    SELECT 'Asignaturas' UNION ALL
    SELECT 'Temas' UNION ALL
    SELECT 'Contenidos' UNION ALL
    SELECT 'Historial_Calificaciones' UNION ALL
    SELECT 'Comentarios_Calificaciones' UNION ALL
    SELECT 'Entregas_Tareas'
)
SELECT
    e.Tabla,
    c.name                              AS Columna_Identity,
    CAST(c.is_identity AS INT)          AS EsIdentity,
    CASE WHEN c.is_identity = 1 THEN 'OK' ELSE 'FALLO' END AS Estado
FROM expected e
LEFT JOIN sys.tables  t ON t.name = e.Tabla AND t.schema_id = SCHEMA_ID('dbo')
LEFT JOIN sys.columns c ON c.object_id = t.object_id AND c.is_identity = 1
ORDER BY e.Tabla;
GO

-- =============================================================================
-- CHECK 2: row counts iguales entre tabla nueva y tabla _OLD
-- =============================================================================
PRINT '=== CHECK 2: row counts nueva vs _OLD ===';

SELECT 'Usuarios' AS Tabla,
       (SELECT COUNT(*) FROM dbo.Usuarios)                       AS RowCount_Nueva,
       (SELECT COUNT(*) FROM dbo.Usuarios_OLD)                   AS RowCount_OLD,
       CASE WHEN (SELECT COUNT(*) FROM dbo.Usuarios) = (SELECT COUNT(*) FROM dbo.Usuarios_OLD) THEN 'OK' ELSE 'FALLO' END AS Estado
UNION ALL
SELECT 'Cursos',
       (SELECT COUNT(*) FROM dbo.Cursos),
       (SELECT COUNT(*) FROM dbo.Cursos_OLD),
       CASE WHEN (SELECT COUNT(*) FROM dbo.Cursos) = (SELECT COUNT(*) FROM dbo.Cursos_OLD) THEN 'OK' ELSE 'FALLO' END
UNION ALL
SELECT 'Asignaturas',
       (SELECT COUNT(*) FROM dbo.Asignaturas),
       (SELECT COUNT(*) FROM dbo.Asignaturas_OLD),
       CASE WHEN (SELECT COUNT(*) FROM dbo.Asignaturas) = (SELECT COUNT(*) FROM dbo.Asignaturas_OLD) THEN 'OK' ELSE 'FALLO' END
UNION ALL
SELECT 'Temas',
       (SELECT COUNT(*) FROM dbo.Temas),
       (SELECT COUNT(*) FROM dbo.Temas_OLD),
       CASE WHEN (SELECT COUNT(*) FROM dbo.Temas) = (SELECT COUNT(*) FROM dbo.Temas_OLD) THEN 'OK' ELSE 'FALLO' END
UNION ALL
SELECT 'Contenidos',
       (SELECT COUNT(*) FROM dbo.Contenidos),
       (SELECT COUNT(*) FROM dbo.Contenidos_OLD),
       CASE WHEN (SELECT COUNT(*) FROM dbo.Contenidos) = (SELECT COUNT(*) FROM dbo.Contenidos_OLD) THEN 'OK' ELSE 'FALLO' END
UNION ALL
SELECT 'Historial_Calificaciones',
       (SELECT COUNT(*) FROM dbo.Historial_Calificaciones),
       (SELECT COUNT(*) FROM dbo.Historial_Calificaciones_OLD),
       CASE WHEN (SELECT COUNT(*) FROM dbo.Historial_Calificaciones) = (SELECT COUNT(*) FROM dbo.Historial_Calificaciones_OLD) THEN 'OK' ELSE 'FALLO' END
UNION ALL
SELECT 'Comentarios_Calificaciones',
       (SELECT COUNT(*) FROM dbo.Comentarios_Calificaciones),
       (SELECT COUNT(*) FROM dbo.Comentarios_Calificaciones_OLD),
       CASE WHEN (SELECT COUNT(*) FROM dbo.Comentarios_Calificaciones) = (SELECT COUNT(*) FROM dbo.Comentarios_Calificaciones_OLD) THEN 'OK' ELSE 'FALLO' END
UNION ALL
SELECT 'Entregas_Tareas',
       (SELECT COUNT(*) FROM dbo.Entregas_Tareas),
       (SELECT COUNT(*) FROM dbo.Entregas_Tareas_OLD),
       CASE WHEN (SELECT COUNT(*) FROM dbo.Entregas_Tareas) = (SELECT COUNT(*) FROM dbo.Entregas_Tareas_OLD) THEN 'OK' ELSE 'FALLO' END;
GO

-- =============================================================================
-- CHECK 3: FKs entrantes recreadas (>= 30)
-- =============================================================================
PRINT '=== CHECK 3: FKs recreadas entrantes a las 8 tablas (esperado >= 30) ===';

DECLARE @fks_actuales INT;
SELECT @fks_actuales = COUNT(*)
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id IN (
    OBJECT_ID('dbo.Usuarios'),
    OBJECT_ID('dbo.Cursos'),
    OBJECT_ID('dbo.Asignaturas'),
    OBJECT_ID('dbo.Temas'),
    OBJECT_ID('dbo.Contenidos'),
    OBJECT_ID('dbo.Comentarios_Calificaciones'),
    OBJECT_ID('dbo.Entregas_Tareas'),
    OBJECT_ID('dbo.Historial_Calificaciones')
);

SELECT
    @fks_actuales                                     AS FKs_Entrantes,
    30                                                AS Mínimo_Esperado,
    CASE WHEN @fks_actuales >= 30 THEN 'OK' ELSE 'FALLO' END AS Estado;

PRINT 'Listado de FKs entrantes:';
SELECT
    OBJECT_NAME(fk.parent_object_id)     AS Tabla_Origen,
    OBJECT_NAME(fk.referenced_object_id) AS Tabla_Destino,
    fk.name                              AS FK_Name
FROM sys.foreign_keys fk
WHERE fk.referenced_object_id IN (
    OBJECT_ID('dbo.Usuarios'),
    OBJECT_ID('dbo.Cursos'),
    OBJECT_ID('dbo.Asignaturas'),
    OBJECT_ID('dbo.Temas'),
    OBJECT_ID('dbo.Contenidos'),
    OBJECT_ID('dbo.Comentarios_Calificaciones'),
    OBJECT_ID('dbo.Entregas_Tareas'),
    OBJECT_ID('dbo.Historial_Calificaciones')
)
ORDER BY OBJECT_NAME(fk.referenced_object_id), OBJECT_NAME(fk.parent_object_id);
GO

-- =============================================================================
-- CHECK 4: índices únicos en Usuarios (Email + Telefono filtrado)
-- =============================================================================
PRINT '=== CHECK 4: Índices únicos en Usuarios ===';

;WITH expected (Idx, EsperaFiltro) AS (
    SELECT 'UQ_Usuarios_Email',  CAST(0 AS BIT) UNION ALL
    SELECT 'IX_Usuarios_Telefono', CAST(1 AS BIT)
)
SELECT
    e.Idx                                  AS Indice_Esperado,
    i.name                                 AS Indice_Actual,
    CAST(i.is_unique   AS INT)             AS EsUnico,
    CAST(i.has_filter  AS INT)             AS TieneFiltro,
    i.filter_definition                    AS DefinicionFiltro,
    CASE
        WHEN i.name IS NULL                                 THEN 'FALLO'
        WHEN i.is_unique = 0                                 THEN 'FALLO'
        WHEN CAST(i.has_filter AS BIT) <> e.EsperaFiltro     THEN 'FALLO'
        ELSE 'OK'
    END                                    AS Estado
FROM expected e
LEFT JOIN sys.indexes i
    ON i.name = e.Idx
   AND i.object_id = OBJECT_ID('dbo.Usuarios');
GO

-- =============================================================================
-- CHECK 5: IDENTITY seed coincide con MAX(ID) actual
-- =============================================================================
PRINT '=== CHECK 5: IDENTITY seeds correctos ===';

DECLARE @seeds TABLE (Tabla NVARCHAR(128), CurrentSeed BIGINT, MaxId BIGINT);

INSERT INTO @seeds VALUES
 ('Usuarios',                   IDENT_CURRENT('dbo.Usuarios'),                   (SELECT ISNULL(MAX(ID_Usuario),0)      FROM dbo.Usuarios)),
 ('Cursos',                     IDENT_CURRENT('dbo.Cursos'),                     (SELECT ISNULL(MAX(ID_Curso),0)        FROM dbo.Cursos)),
 ('Asignaturas',                IDENT_CURRENT('dbo.Asignaturas'),                (SELECT ISNULL(MAX(ID_Asignatura),0)   FROM dbo.Asignaturas)),
 ('Temas',                      IDENT_CURRENT('dbo.Temas'),                      (SELECT ISNULL(MAX(ID_Tema),0)         FROM dbo.Temas)),
 ('Contenidos',                 IDENT_CURRENT('dbo.Contenidos'),                 (SELECT ISNULL(MAX(ID_Contenido),0)    FROM dbo.Contenidos)),
 ('Historial_Calificaciones',   IDENT_CURRENT('dbo.Historial_Calificaciones'),   (SELECT ISNULL(MAX(ID_Calificacion),0) FROM dbo.Historial_Calificaciones)),
 ('Comentarios_Calificaciones', IDENT_CURRENT('dbo.Comentarios_Calificaciones'), (SELECT ISNULL(MAX(ID_Comentario),0)   FROM dbo.Comentarios_Calificaciones)),
 ('Entregas_Tareas',            IDENT_CURRENT('dbo.Entregas_Tareas'),            (SELECT ISNULL(MAX(ID_Entrega),0)      FROM dbo.Entregas_Tareas));

SELECT
    Tabla,
    CurrentSeed,
    MaxId,
    CASE WHEN CurrentSeed >= MaxId THEN 'OK' ELSE 'FALLO' END AS Estado
FROM @seeds
ORDER BY Tabla;
GO

PRINT '=== Verificación finalizada ===';
GO
