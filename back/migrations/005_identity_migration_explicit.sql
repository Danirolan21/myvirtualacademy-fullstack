-- =============================================================================
-- 005_identity_migration_explicit.sql
-- Versión alternativa de 005_identity_migration.sql para Azure Query Editor.
--
-- Diferencias respecto a 005_identity_migration.sql:
--   * Sin USE [MyVirtualAcademy]; GO al principio (Azure Query Editor ya tiene
--     contexto de BD).
--   * FASE 1 con 31 sentencias ALTER TABLE ... DROP CONSTRAINT explícitas,
--     usando los nombres reales de FK en Azure SQL (consultados directamente
--     sobre la instancia destino). Esto evita el patrón dinámico con QUOTENAME
--     que Azure Query Editor rechazaba.
--   * Drop explícito de los 2 índices únicos de Usuarios con sus nombres
--     reales en Azure (UQ__Usuarios__A9D1053409A58ACD, IX_Usuarios_Telefono).
--   * Sin GO final.
--
-- FASE 2, 3 y 4 son idénticas a 005_identity_migration.sql.
-- =============================================================================

SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- =========================================================================
    -- FASE 1: Drop FKs entrantes + índices únicos de Usuarios
    -- 31 FKs entrantes a las 8 tablas afectadas — nombres reales consultados
    -- en Azure SQL contra sys.foreign_keys el día 2026-05-15.
    -- =========================================================================
    PRINT '--- FASE 1: Drop FKs entrantes e índices únicos de Usuarios ---';

    -- Asignaturas → Cursos
    ALTER TABLE [dbo].[Asignaturas]                DROP CONSTRAINT [FK__Asignatur__ID_Cu__5441852A];

    -- Comentarios_Calificaciones → Usuarios (ID_Autor), Historial_Calificaciones (ID_Calificacion)
    ALTER TABLE [dbo].[Comentarios_Calificaciones] DROP CONSTRAINT [FK__Comentari__ID_Au__44CA3770];
    ALTER TABLE [dbo].[Comentarios_Calificaciones] DROP CONSTRAINT [FK__Comentari__ID_Ca__45BE5BA9];

    -- Contenidos → Temas
    ALTER TABLE [dbo].[Contenidos]                 DROP CONSTRAINT [FK__Contenido__ID_Te__46B27FE2];

    -- Correcciones → Usuarios
    ALTER TABLE [dbo].[Correcciones]               DROP CONSTRAINT [FK__Correccio__ID_Pr__47A6A41B];

    -- Cursos → Usuarios (ID_Profesor x1, ID_Profesor_Suplente x2 — duplicado heredado)
    ALTER TABLE [dbo].[Cursos]                     DROP CONSTRAINT [FK__Cursos__ID_Profe__5165187F];
    ALTER TABLE [dbo].[Cursos]                     DROP CONSTRAINT [FK_Cursos_Profesor];
    ALTER TABLE [dbo].[Cursos]                     DROP CONSTRAINT [FK_Cursos_ProfesorSuplente];

    -- Entregas_Tareas → Contenidos, Usuarios
    ALTER TABLE [dbo].[Entregas_Tareas]            DROP CONSTRAINT [FK__Entregas___ID_Co__4C6B5938];
    ALTER TABLE [dbo].[Entregas_Tareas]            DROP CONSTRAINT [FK__Entregas___ID_Es__4D5F7D71];

    -- Examenes → Contenidos
    ALTER TABLE [dbo].[Examenes]                   DROP CONSTRAINT [FK__Examenes__ID_Con__4E53A1AA];

    -- Examenes_Usuarios → Contenidos, Usuarios
    ALTER TABLE [dbo].[Examenes_Usuarios]          DROP CONSTRAINT [FK__Examenes___ID_Co__4F47C5E3];
    ALTER TABLE [dbo].[Examenes_Usuarios]          DROP CONSTRAINT [FK__Examenes___ID_Es__503BEA1C];

    -- Historial_Calificaciones → Contenidos, Usuarios (ID_Estudiante, ID_Profesor_Calificador)
    ALTER TABLE [dbo].[Historial_Calificaciones]   DROP CONSTRAINT [FK__Historial__ID_Co__51300E55];
    ALTER TABLE [dbo].[Historial_Calificaciones]   DROP CONSTRAINT [FK__Historial__ID_Es__5224328E];
    ALTER TABLE [dbo].[Historial_Calificaciones]   DROP CONSTRAINT [FK__Historial__ID_Pr__531856C7];

    -- Inscripciones → Cursos, Usuarios
    ALTER TABLE [dbo].[Inscripciones]              DROP CONSTRAINT [FK__Inscripci__ID_Cu__04E4BC85];
    ALTER TABLE [dbo].[Inscripciones]              DROP CONSTRAINT [FK__Inscripci__ID_Es__55009F39];

    -- Mensajes_Chat → Usuarios
    ALTER TABLE [dbo].[Mensajes_Chat]              DROP CONSTRAINT [FK__Mensajes___ID_Em__55F4C372];

    -- Miembros_Grupo → Usuarios
    ALTER TABLE [dbo].[Miembros_Grupo]             DROP CONSTRAINT [FK__Miembros___ID_Us__58D1301D];

    -- Notificaciones → Usuarios (Enviado_Por, ID_Usuario)
    ALTER TABLE [dbo].[Notificaciones]             DROP CONSTRAINT [FK__Notificac__Envia__59C55456];
    ALTER TABLE [dbo].[Notificaciones]             DROP CONSTRAINT [FK__Notificac__ID_Us__5AB9788F];

    -- Preguntas → Contenidos
    ALTER TABLE [dbo].[Preguntas]                  DROP CONSTRAINT [FK__Preguntas__ID_Co__5BAD9CC8];

    -- Profesores_Asignaturas → Asignaturas, Usuarios
    ALTER TABLE [dbo].[Profesores_Asignaturas]     DROP CONSTRAINT [FK__Profesore__ID_As__5CA1C101];
    ALTER TABLE [dbo].[Profesores_Asignaturas]     DROP CONSTRAINT [FK_ProfesoresAsignaturas_Profesor];

    -- Progreso_Inscripciones → Contenidos
    ALTER TABLE [dbo].[Progreso_Inscripciones]     DROP CONSTRAINT [FK__Progreso___ID_Co__5E8A0973];

    -- RefreshTokens → Usuarios
    ALTER TABLE [dbo].[RefreshTokens]              DROP CONSTRAINT [FK__RefreshTo__IdUsu__607251E5];

    -- Respuestas_Desarrollo → Usuarios
    ALTER TABLE [dbo].[Respuestas_Desarrollo]      DROP CONSTRAINT [FK__Respuesta__ID_Es__625A9A57];

    -- Respuestas_Usuarios → Usuarios
    ALTER TABLE [dbo].[Respuestas_Usuarios]        DROP CONSTRAINT [FK__Respuesta__ID_Es__65370702];

    -- Temas → Asignaturas
    ALTER TABLE [dbo].[Temas]                      DROP CONSTRAINT [FK__Temas__ID_Asigna__690797E6];

    -- Usuarios_Roles → Usuarios
    ALTER TABLE [dbo].[Usuarios_Roles]             DROP CONSTRAINT [FK_UsuariosRoles_Usuario];

    -- Índices únicos de Usuarios
    DROP INDEX [IX_Usuarios_Telefono] ON [dbo].[Usuarios];
    ALTER TABLE [dbo].[Usuarios] DROP CONSTRAINT [UQ__Usuarios__A9D1053409A58ACD];

    -- =========================================================================
    -- FASE 2: Para cada tabla → sp_rename → CREATE IDENTITY → INSERT → RESEED
    -- =========================================================================
    PRINT '--- FASE 2: Recrear 8 tablas con IDENTITY ---';

    DECLARE @reseed NVARCHAR(MAX);
    DECLARE @max_id INT;

    ----------------------------------------------------------------------------
    -- 2.1  Usuarios  (incluye columnas añadidas por migraciones 001 y 004)
    ----------------------------------------------------------------------------
    EXEC sp_rename 'dbo.Usuarios', 'Usuarios_OLD';

    CREATE TABLE [dbo].[Usuarios](
        [ID_Usuario]          INT IDENTITY(1,1) NOT NULL,
        [Nombre]              NVARCHAR(50)   NOT NULL,
        [Apellidos]           NVARCHAR(100)  NOT NULL,
        [Email]               NVARCHAR(100)  NOT NULL,
        [Pass_Hash]           VARBINARY(MAX) NOT NULL,
        [Salt]                NVARCHAR(50)   NOT NULL,
        [Pass]                NVARCHAR(255)  NOT NULL,
        [Fecha_Registro]      DATETIME       NULL,
        [Activo]              BIT            NULL,
        [Ultimo_Acceso]       DATETIME       NULL,
        [Foto_Perfil]         NVARCHAR(255)  NULL,
        [Telefono]            NVARCHAR(20)   NULL,
        [Token_Recuperacion]  NVARCHAR(255)  NULL,
        [Pass_BCrypt]         NVARCHAR(255)  NULL,
        [MigratedToBCrypt]    BIT            NOT NULL CONSTRAINT [DF_Usuarios_MigratedToBCrypt] DEFAULT (0),
        [IsAdmin]             BIT            NOT NULL CONSTRAINT [DF_Usuarios_IsAdmin]          DEFAULT (0),
        CONSTRAINT [PK_Usuarios] PRIMARY KEY CLUSTERED ([ID_Usuario] ASC)
    );

    SET IDENTITY_INSERT [dbo].[Usuarios] ON;
    INSERT INTO [dbo].[Usuarios]
        ([ID_Usuario],[Nombre],[Apellidos],[Email],[Pass_Hash],[Salt],[Pass],
         [Fecha_Registro],[Activo],[Ultimo_Acceso],[Foto_Perfil],[Telefono],[Token_Recuperacion],
         [Pass_BCrypt],[MigratedToBCrypt],[IsAdmin])
    SELECT
        [ID_Usuario],[Nombre],[Apellidos],[Email],[Pass_Hash],[Salt],[Pass],
        [Fecha_Registro],[Activo],[Ultimo_Acceso],[Foto_Perfil],[Telefono],[Token_Recuperacion],
        [Pass_BCrypt],[MigratedToBCrypt],[IsAdmin]
    FROM [dbo].[Usuarios_OLD];
    SET IDENTITY_INSERT [dbo].[Usuarios] OFF;

    SET @max_id = (SELECT ISNULL(MAX([ID_Usuario]), 0) FROM [dbo].[Usuarios]);
    SET @reseed = N'DBCC CHECKIDENT (''dbo.Usuarios'', RESEED, ' + CAST(@max_id AS NVARCHAR(20)) + N')';
    EXEC sp_executesql @reseed;

    ----------------------------------------------------------------------------
    -- 2.2  Cursos
    ----------------------------------------------------------------------------
    EXEC sp_rename 'dbo.Cursos', 'Cursos_OLD';

    CREATE TABLE [dbo].[Cursos](
        [ID_Curso]              INT IDENTITY(1,1) NOT NULL,
        [Nombre]                NVARCHAR(100)  NOT NULL,
        [Descripcion]           NVARCHAR(MAX)  NULL,
        [ID_Profesor]           INT            NOT NULL,
        [ID_Profesor_Suplente]  INT            NULL,
        [Fecha_Inicio]          DATE           NOT NULL,
        [Fecha_Fin]             DATE           NOT NULL,
        [Estado]                VARCHAR(20)    NOT NULL CONSTRAINT [DF_Cursos_Estado] DEFAULT ('Borrador'),
        [Imagen_Portada]        NVARCHAR(255)  NULL,
        CONSTRAINT [PK_Cursos] PRIMARY KEY CLUSTERED ([ID_Curso] ASC)
    );

    SET IDENTITY_INSERT [dbo].[Cursos] ON;
    INSERT INTO [dbo].[Cursos]
        ([ID_Curso],[Nombre],[Descripcion],[ID_Profesor],[ID_Profesor_Suplente],
         [Fecha_Inicio],[Fecha_Fin],[Estado],[Imagen_Portada])
    SELECT
        [ID_Curso],[Nombre],[Descripcion],[ID_Profesor],[ID_Profesor_Suplente],
        [Fecha_Inicio],[Fecha_Fin],[Estado],[Imagen_Portada]
    FROM [dbo].[Cursos_OLD];
    SET IDENTITY_INSERT [dbo].[Cursos] OFF;

    SET @max_id = (SELECT ISNULL(MAX([ID_Curso]), 0) FROM [dbo].[Cursos]);
    SET @reseed = N'DBCC CHECKIDENT (''dbo.Cursos'', RESEED, ' + CAST(@max_id AS NVARCHAR(20)) + N')';
    EXEC sp_executesql @reseed;

    ----------------------------------------------------------------------------
    -- 2.3  Asignaturas
    ----------------------------------------------------------------------------
    EXEC sp_rename 'dbo.Asignaturas', 'Asignaturas_OLD';

    CREATE TABLE [dbo].[Asignaturas](
        [ID_Asignatura] INT IDENTITY(1,1) NOT NULL,
        [ID_Curso]      INT           NOT NULL,
        [Nombre]        NVARCHAR(100) NOT NULL,
        CONSTRAINT [PK_Asignaturas] PRIMARY KEY CLUSTERED ([ID_Asignatura] ASC)
    );

    SET IDENTITY_INSERT [dbo].[Asignaturas] ON;
    INSERT INTO [dbo].[Asignaturas] ([ID_Asignatura],[ID_Curso],[Nombre])
    SELECT [ID_Asignatura],[ID_Curso],[Nombre] FROM [dbo].[Asignaturas_OLD];
    SET IDENTITY_INSERT [dbo].[Asignaturas] OFF;

    SET @max_id = (SELECT ISNULL(MAX([ID_Asignatura]), 0) FROM [dbo].[Asignaturas]);
    SET @reseed = N'DBCC CHECKIDENT (''dbo.Asignaturas'', RESEED, ' + CAST(@max_id AS NVARCHAR(20)) + N')';
    EXEC sp_executesql @reseed;

    ----------------------------------------------------------------------------
    -- 2.4  Temas
    ----------------------------------------------------------------------------
    EXEC sp_rename 'dbo.Temas', 'Temas_OLD';

    CREATE TABLE [dbo].[Temas](
        [ID_Tema]       INT IDENTITY(1,1) NOT NULL,
        [ID_Asignatura] INT           NOT NULL,
        [Nombre]        NVARCHAR(200) NOT NULL,
        [Orden]         INT           NOT NULL,
        CONSTRAINT [PK_Temas] PRIMARY KEY CLUSTERED ([ID_Tema] ASC)
    );

    SET IDENTITY_INSERT [dbo].[Temas] ON;
    INSERT INTO [dbo].[Temas] ([ID_Tema],[ID_Asignatura],[Nombre],[Orden])
    SELECT [ID_Tema],[ID_Asignatura],[Nombre],[Orden] FROM [dbo].[Temas_OLD];
    SET IDENTITY_INSERT [dbo].[Temas] OFF;

    SET @max_id = (SELECT ISNULL(MAX([ID_Tema]), 0) FROM [dbo].[Temas]);
    SET @reseed = N'DBCC CHECKIDENT (''dbo.Temas'', RESEED, ' + CAST(@max_id AS NVARCHAR(20)) + N')';
    EXEC sp_executesql @reseed;

    ----------------------------------------------------------------------------
    -- 2.5  Contenidos
    ----------------------------------------------------------------------------
    EXEC sp_rename 'dbo.Contenidos', 'Contenidos_OLD';

    CREATE TABLE [dbo].[Contenidos](
        [ID_Contenido]      INT IDENTITY(1,1) NOT NULL,
        [ID_Tema]           INT            NOT NULL,
        [Titulo]            NVARCHAR(200)  NOT NULL,
        [Tipo]              VARCHAR(20)    NOT NULL,
        [URL_Contenido]     NVARCHAR(255)  NULL,
        [Descripcion]       NVARCHAR(MAX)  NULL,
        [Orden]             INT            NOT NULL,
        [Fecha_Publicacion] DATETIME       NULL CONSTRAINT [DF_Contenidos_FechaPub] DEFAULT (GETDATE()),
        [Fecha_Entrega]     DATETIME       NULL,
        [Puntuacion_Maxima] DECIMAL(5,2)   NULL,
        CONSTRAINT [PK_Contenidos] PRIMARY KEY CLUSTERED ([ID_Contenido] ASC)
    );

    SET IDENTITY_INSERT [dbo].[Contenidos] ON;
    INSERT INTO [dbo].[Contenidos]
        ([ID_Contenido],[ID_Tema],[Titulo],[Tipo],[URL_Contenido],[Descripcion],
         [Orden],[Fecha_Publicacion],[Fecha_Entrega],[Puntuacion_Maxima])
    SELECT
        [ID_Contenido],[ID_Tema],[Titulo],[Tipo],[URL_Contenido],[Descripcion],
        [Orden],[Fecha_Publicacion],[Fecha_Entrega],[Puntuacion_Maxima]
    FROM [dbo].[Contenidos_OLD];
    SET IDENTITY_INSERT [dbo].[Contenidos] OFF;

    SET @max_id = (SELECT ISNULL(MAX([ID_Contenido]), 0) FROM [dbo].[Contenidos]);
    SET @reseed = N'DBCC CHECKIDENT (''dbo.Contenidos'', RESEED, ' + CAST(@max_id AS NVARCHAR(20)) + N')';
    EXEC sp_executesql @reseed;

    ----------------------------------------------------------------------------
    -- 2.6  Historial_Calificaciones
    ----------------------------------------------------------------------------
    EXEC sp_rename 'dbo.Historial_Calificaciones', 'Historial_Calificaciones_OLD';

    CREATE TABLE [dbo].[Historial_Calificaciones](
        [ID_Calificacion]         INT IDENTITY(1,1) NOT NULL,
        [ID_Estudiante]           INT          NOT NULL,
        [ID_Contenido]            INT          NOT NULL,
        [Calificacion]            DECIMAL(5,2) NOT NULL,
        [Fecha_Calificacion]      DATETIME     NULL CONSTRAINT [DF_HistCalif_Fecha] DEFAULT (GETDATE()),
        [ID_Profesor_Calificador] INT          NULL,
        CONSTRAINT [PK_Historial_Calificaciones] PRIMARY KEY CLUSTERED ([ID_Calificacion] ASC)
    );

    SET IDENTITY_INSERT [dbo].[Historial_Calificaciones] ON;
    INSERT INTO [dbo].[Historial_Calificaciones]
        ([ID_Calificacion],[ID_Estudiante],[ID_Contenido],[Calificacion],
         [Fecha_Calificacion],[ID_Profesor_Calificador])
    SELECT
        [ID_Calificacion],[ID_Estudiante],[ID_Contenido],[Calificacion],
        [Fecha_Calificacion],[ID_Profesor_Calificador]
    FROM [dbo].[Historial_Calificaciones_OLD];
    SET IDENTITY_INSERT [dbo].[Historial_Calificaciones] OFF;

    SET @max_id = (SELECT ISNULL(MAX([ID_Calificacion]), 0) FROM [dbo].[Historial_Calificaciones]);
    SET @reseed = N'DBCC CHECKIDENT (''dbo.Historial_Calificaciones'', RESEED, ' + CAST(@max_id AS NVARCHAR(20)) + N')';
    EXEC sp_executesql @reseed;

    ----------------------------------------------------------------------------
    -- 2.7  Comentarios_Calificaciones
    ----------------------------------------------------------------------------
    EXEC sp_rename 'dbo.Comentarios_Calificaciones', 'Comentarios_Calificaciones_OLD';

    CREATE TABLE [dbo].[Comentarios_Calificaciones](
        [ID_Comentario]    INT IDENTITY(1,1) NOT NULL,
        [ID_Calificacion]  INT           NOT NULL,
        [ID_Autor]         INT           NOT NULL,
        [Comentario]       NVARCHAR(MAX) NOT NULL,
        [Fecha_Comentario] DATETIME      NULL CONSTRAINT [DF_ComentCalif_Fecha] DEFAULT (GETDATE()),
        CONSTRAINT [PK_Comentarios_Calificaciones] PRIMARY KEY CLUSTERED ([ID_Comentario] ASC)
    );

    SET IDENTITY_INSERT [dbo].[Comentarios_Calificaciones] ON;
    INSERT INTO [dbo].[Comentarios_Calificaciones]
        ([ID_Comentario],[ID_Calificacion],[ID_Autor],[Comentario],[Fecha_Comentario])
    SELECT
        [ID_Comentario],[ID_Calificacion],[ID_Autor],[Comentario],[Fecha_Comentario]
    FROM [dbo].[Comentarios_Calificaciones_OLD];
    SET IDENTITY_INSERT [dbo].[Comentarios_Calificaciones] OFF;

    SET @max_id = (SELECT ISNULL(MAX([ID_Comentario]), 0) FROM [dbo].[Comentarios_Calificaciones]);
    SET @reseed = N'DBCC CHECKIDENT (''dbo.Comentarios_Calificaciones'', RESEED, ' + CAST(@max_id AS NVARCHAR(20)) + N')';
    EXEC sp_executesql @reseed;

    ----------------------------------------------------------------------------
    -- 2.8  Entregas_Tareas
    ----------------------------------------------------------------------------
    EXEC sp_rename 'dbo.Entregas_Tareas', 'Entregas_Tareas_OLD';

    CREATE TABLE [dbo].[Entregas_Tareas](
        [ID_Entrega]    INT IDENTITY(1,1) NOT NULL,
        [ID_Contenido]  INT           NOT NULL,
        [ID_Estudiante] INT           NULL,
        [URL_Entrega]   NVARCHAR(255) NULL,
        [Fecha_Entrega] DATETIME      NULL CONSTRAINT [DF_EntregasTareas_Fecha]  DEFAULT (GETDATE()),
        [Estado]        VARCHAR(20)   NULL CONSTRAINT [DF_EntregasTareas_Estado] DEFAULT ('Pendiente'),
        CONSTRAINT [PK_Entregas_Tareas] PRIMARY KEY CLUSTERED ([ID_Entrega] ASC)
    );

    SET IDENTITY_INSERT [dbo].[Entregas_Tareas] ON;
    INSERT INTO [dbo].[Entregas_Tareas]
        ([ID_Entrega],[ID_Contenido],[ID_Estudiante],[URL_Entrega],[Fecha_Entrega],[Estado])
    SELECT
        [ID_Entrega],[ID_Contenido],[ID_Estudiante],[URL_Entrega],[Fecha_Entrega],[Estado]
    FROM [dbo].[Entregas_Tareas_OLD];
    SET IDENTITY_INSERT [dbo].[Entregas_Tareas] OFF;

    SET @max_id = (SELECT ISNULL(MAX([ID_Entrega]), 0) FROM [dbo].[Entregas_Tareas]);
    SET @reseed = N'DBCC CHECKIDENT (''dbo.Entregas_Tareas'', RESEED, ' + CAST(@max_id AS NVARCHAR(20)) + N')';
    EXEC sp_executesql @reseed;

    -- =========================================================================
    -- FASE 3: Recrear índices únicos de Usuarios + 30 FKs
    -- =========================================================================
    PRINT '--- FASE 3: Recrear índices únicos de Usuarios y FKs ---';

    -- Índices únicos de Usuarios
    ALTER TABLE [dbo].[Usuarios]
        ADD CONSTRAINT [UQ_Usuarios_Email] UNIQUE NONCLUSTERED ([Email] ASC);

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Usuarios_Telefono]
        ON [dbo].[Usuarios] ([Telefono] ASC)
        WHERE ([Telefono] IS NOT NULL);

    -- FKs entrantes a Cursos
    ALTER TABLE [dbo].[Asignaturas]   ADD CONSTRAINT [FK_Asignaturas_Cursos]   FOREIGN KEY ([ID_Curso]) REFERENCES [dbo].[Cursos]([ID_Curso]);
    ALTER TABLE [dbo].[Inscripciones] ADD CONSTRAINT [FK_Inscripciones_Cursos] FOREIGN KEY ([ID_Curso]) REFERENCES [dbo].[Cursos]([ID_Curso]);

    -- FKs entrantes a Asignaturas
    ALTER TABLE [dbo].[Profesores_Asignaturas] ADD CONSTRAINT [FK_ProfesoresAsignaturas_Asignaturas] FOREIGN KEY ([ID_Asignatura]) REFERENCES [dbo].[Asignaturas]([ID_Asignatura]);
    ALTER TABLE [dbo].[Temas]                  ADD CONSTRAINT [FK_Temas_Asignaturas]                 FOREIGN KEY ([ID_Asignatura]) REFERENCES [dbo].[Asignaturas]([ID_Asignatura]);

    -- FKs entrantes a Temas
    ALTER TABLE [dbo].[Contenidos] ADD CONSTRAINT [FK_Contenidos_Temas] FOREIGN KEY ([ID_Tema]) REFERENCES [dbo].[Temas]([ID_Tema]);

    -- FKs entrantes a Contenidos
    ALTER TABLE [dbo].[Entregas_Tareas]          ADD CONSTRAINT [FK_EntregasTareas_Contenidos]        FOREIGN KEY ([ID_Contenido]) REFERENCES [dbo].[Contenidos]([ID_Contenido]);
    ALTER TABLE [dbo].[Examenes]                 ADD CONSTRAINT [FK_Examenes_Contenidos]              FOREIGN KEY ([ID_Contenido]) REFERENCES [dbo].[Contenidos]([ID_Contenido]);
    ALTER TABLE [dbo].[Examenes_Usuarios]        ADD CONSTRAINT [FK_ExamenesUsuarios_Contenidos]      FOREIGN KEY ([ID_Contenido]) REFERENCES [dbo].[Contenidos]([ID_Contenido]);
    ALTER TABLE [dbo].[Historial_Calificaciones] ADD CONSTRAINT [FK_HistorialCalif_Contenidos]        FOREIGN KEY ([ID_Contenido]) REFERENCES [dbo].[Contenidos]([ID_Contenido]);
    ALTER TABLE [dbo].[Preguntas]                ADD CONSTRAINT [FK_Preguntas_Contenidos]             FOREIGN KEY ([ID_Contenido]) REFERENCES [dbo].[Contenidos]([ID_Contenido]);
    ALTER TABLE [dbo].[Progreso_Inscripciones]   ADD CONSTRAINT [FK_ProgresoInscripciones_Contenidos] FOREIGN KEY ([ID_Contenido]) REFERENCES [dbo].[Contenidos]([ID_Contenido]);

    -- FKs entrantes a Historial_Calificaciones
    ALTER TABLE [dbo].[Comentarios_Calificaciones] ADD CONSTRAINT [FK_ComentariosCalif_HistorialCalif] FOREIGN KEY ([ID_Calificacion]) REFERENCES [dbo].[Historial_Calificaciones]([ID_Calificacion]);

    -- FKs entrantes a Usuarios
    ALTER TABLE [dbo].[Comentarios_Calificaciones] ADD CONSTRAINT [FK_ComentariosCalif_Usuarios]       FOREIGN KEY ([ID_Autor])                REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Correcciones]               ADD CONSTRAINT [FK_Correcciones_Usuarios]          FOREIGN KEY ([ID_Profesor])             REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Cursos]                     ADD CONSTRAINT [FK_Cursos_Profesor]                FOREIGN KEY ([ID_Profesor])             REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Cursos]                     ADD CONSTRAINT [FK_Cursos_ProfesorSuplente]        FOREIGN KEY ([ID_Profesor_Suplente])    REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Entregas_Tareas]            ADD CONSTRAINT [FK_EntregasTareas_Usuarios]        FOREIGN KEY ([ID_Estudiante])           REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Examenes_Usuarios]          ADD CONSTRAINT [FK_ExamenesUsuarios_Usuarios]      FOREIGN KEY ([ID_Estudiante])           REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Historial_Calificaciones]   ADD CONSTRAINT [FK_HistorialCalif_Estudiante]      FOREIGN KEY ([ID_Estudiante])           REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Historial_Calificaciones]   ADD CONSTRAINT [FK_HistorialCalif_ProfCalificador] FOREIGN KEY ([ID_Profesor_Calificador]) REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Inscripciones]              ADD CONSTRAINT [FK_Inscripciones_Usuarios]         FOREIGN KEY ([ID_Estudiante])           REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Mensajes_Chat]              ADD CONSTRAINT [FK_MensajesChat_Usuarios]          FOREIGN KEY ([ID_Emisor])               REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Miembros_Grupo]             ADD CONSTRAINT [FK_MiembrosGrupo_Usuarios]         FOREIGN KEY ([ID_Usuario])              REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Notificaciones]             ADD CONSTRAINT [FK_Notificaciones_Usuario]         FOREIGN KEY ([ID_Usuario])              REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Notificaciones]             ADD CONSTRAINT [FK_Notificaciones_EnviadoPor]      FOREIGN KEY ([Enviado_Por])             REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Profesores_Asignaturas]     ADD CONSTRAINT [FK_ProfesoresAsignaturas_Usuarios] FOREIGN KEY ([ID_Profesor])             REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Respuestas_Desarrollo]      ADD CONSTRAINT [FK_RespuestasDesarrollo_Usuarios]  FOREIGN KEY ([ID_Estudiante])           REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Respuestas_Usuarios]        ADD CONSTRAINT [FK_RespuestasUsuarios_Usuarios]    FOREIGN KEY ([ID_Estudiante])           REFERENCES [dbo].[Usuarios]([ID_Usuario]);
    ALTER TABLE [dbo].[Usuarios_Roles]             ADD CONSTRAINT [FK_UsuariosRoles_Usuarios]         FOREIGN KEY ([ID_Usuario])              REFERENCES [dbo].[Usuarios]([ID_Usuario]);

    -- FK adicional de migración 001 (condicional)
    IF OBJECT_ID('dbo.RefreshTokens') IS NOT NULL
        ALTER TABLE [dbo].[RefreshTokens]
            ADD CONSTRAINT [FK_RefreshTokens_Usuarios]
            FOREIGN KEY ([IdUsuario]) REFERENCES [dbo].[Usuarios]([ID_Usuario]);

    -- =========================================================================
    -- FASE 4: sp_refreshview en las 5 vistas registradas
    -- =========================================================================
    PRINT '--- FASE 4: sp_refreshview ---';

    EXEC sp_refreshview N'dbo.Vista_Usuarios_Con_Roles';
    EXEC sp_refreshview N'dbo.Vista_Inscripciones';
    EXEC sp_refreshview N'dbo.Vista_Asignaturas_Usuario';
    EXEC sp_refreshview N'dbo.Vista_Cursos_Detalles';
    EXEC sp_refreshview N'dbo.Vista_Detalles_Asignatura';

    COMMIT TRANSACTION;
    PRINT 'Migración completada';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

    DECLARE @errmsg  NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @errnum  INT            = ERROR_NUMBER();
    DECLARE @errline INT            = ERROR_LINE();
    PRINT 'ERROR ' + CAST(@errnum AS NVARCHAR) + ' (línea ' + CAST(@errline AS NVARCHAR) + '): ' + @errmsg;
    THROW;
END CATCH;
