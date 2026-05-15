-- =============================================================================
-- 005_identity_rollback.sql
-- Revierte la migración 005 restaurando las tablas _OLD al nombre original.
-- Solo es válido si todavía existen las tablas *_OLD (i.e. ANTES de ejecutar
-- 005_identity_cleanup.sql).
--
-- Tras este script las tablas vuelven a NO tener IDENTITY (estado pre-migración),
-- las FKs originales (vía sys.foreign_keys) son recreadas con nombres limpios,
-- y los unique indexes de Usuarios se restauran. Las vistas se refrescan.
-- =============================================================================

USE [MyVirtualAcademy];
GO

SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    -- =========================================================================
    -- Pre-check: las 8 tablas _OLD deben existir
    -- =========================================================================
    IF  OBJECT_ID('dbo.Usuarios_OLD')                   IS NULL
     OR OBJECT_ID('dbo.Cursos_OLD')                     IS NULL
     OR OBJECT_ID('dbo.Asignaturas_OLD')                IS NULL
     OR OBJECT_ID('dbo.Temas_OLD')                      IS NULL
     OR OBJECT_ID('dbo.Contenidos_OLD')                 IS NULL
     OR OBJECT_ID('dbo.Historial_Calificaciones_OLD')   IS NULL
     OR OBJECT_ID('dbo.Comentarios_Calificaciones_OLD') IS NULL
     OR OBJECT_ID('dbo.Entregas_Tareas_OLD')            IS NULL
    BEGIN
        RAISERROR('Rollback abortado: una o más tablas *_OLD no existen. ¿Ya ejecutaste cleanup?', 16, 1);
    END

    -- =========================================================================
    -- FASE 1: Drop FKs entrantes a las 8 tablas IDENTITY recién creadas
    -- =========================================================================
    PRINT '--- FASE 1: Drop FKs entrantes a tablas IDENTITY ---';

    DECLARE @drop_fks NVARCHAR(MAX) = N'';
    SELECT @drop_fks = @drop_fks
                     + 'ALTER TABLE ' + QUOTENAME(SCHEMA_NAME(t.schema_id)) + '.' + QUOTENAME(t.name)
                     + ' DROP CONSTRAINT ' + QUOTENAME(fk.name) + ';' + CHAR(10)
    FROM sys.foreign_keys fk
    INNER JOIN sys.tables t ON fk.parent_object_id = t.object_id
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
    EXEC sp_executesql @drop_fks;

    -- Drop unique indexes de Usuarios (creados en Fase 3 de la migración)
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Usuarios_Telefono' AND object_id = OBJECT_ID('dbo.Usuarios'))
        DROP INDEX [IX_Usuarios_Telefono] ON [dbo].[Usuarios];

    IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_Usuarios_Email' AND parent_object_id = OBJECT_ID('dbo.Usuarios'))
        ALTER TABLE [dbo].[Usuarios] DROP CONSTRAINT [UQ_Usuarios_Email];

    -- =========================================================================
    -- FASE 2: Drop las 8 tablas IDENTITY (orden inverso al de Fase 2 de migration)
    -- =========================================================================
    PRINT '--- FASE 2: Drop tablas IDENTITY ---';

    DROP TABLE [dbo].[Entregas_Tareas];
    DROP TABLE [dbo].[Comentarios_Calificaciones];
    DROP TABLE [dbo].[Historial_Calificaciones];
    DROP TABLE [dbo].[Contenidos];
    DROP TABLE [dbo].[Temas];
    DROP TABLE [dbo].[Asignaturas];
    DROP TABLE [dbo].[Cursos];
    DROP TABLE [dbo].[Usuarios];

    -- =========================================================================
    -- FASE 3: sp_rename *_OLD → nombres originales
    -- =========================================================================
    PRINT '--- FASE 3: sp_rename _OLD → nombres originales ---';

    EXEC sp_rename 'dbo.Usuarios_OLD',                   'Usuarios';
    EXEC sp_rename 'dbo.Cursos_OLD',                     'Cursos';
    EXEC sp_rename 'dbo.Asignaturas_OLD',                'Asignaturas';
    EXEC sp_rename 'dbo.Temas_OLD',                      'Temas';
    EXEC sp_rename 'dbo.Contenidos_OLD',                 'Contenidos';
    EXEC sp_rename 'dbo.Historial_Calificaciones_OLD',   'Historial_Calificaciones';
    EXEC sp_rename 'dbo.Comentarios_Calificaciones_OLD', 'Comentarios_Calificaciones';
    EXEC sp_rename 'dbo.Entregas_Tareas_OLD',            'Entregas_Tareas';

    -- =========================================================================
    -- FASE 4: Recrear unique indexes de Usuarios (estado pre-migración)
    -- =========================================================================
    PRINT '--- FASE 4: Recrear unique indexes de Usuarios ---';

    ALTER TABLE [dbo].[Usuarios]
        ADD CONSTRAINT [UQ_Usuarios_Email] UNIQUE NONCLUSTERED ([Email] ASC);

    CREATE UNIQUE NONCLUSTERED INDEX [IX_Usuarios_Telefono]
        ON [dbo].[Usuarios] ([Telefono] ASC)
        WHERE ([Telefono] IS NOT NULL);

    -- =========================================================================
    -- FASE 5: Recrear las 30 FKs apuntando a las tablas restauradas
    -- =========================================================================
    PRINT '--- FASE 5: Recrear FKs ---';

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
    -- FASE 6: sp_refreshview en las 5 vistas
    -- =========================================================================
    PRINT '--- FASE 6: sp_refreshview ---';

    EXEC sp_refreshview N'dbo.Vista_Usuarios_Con_Roles';
    EXEC sp_refreshview N'dbo.Vista_Inscripciones';
    EXEC sp_refreshview N'dbo.Vista_Asignaturas_Usuario';
    EXEC sp_refreshview N'dbo.Vista_Cursos_Detalles';
    EXEC sp_refreshview N'dbo.Vista_Detalles_Asignatura';

    COMMIT TRANSACTION;
    PRINT 'Rollback completado';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

    DECLARE @errmsg  NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @errnum  INT            = ERROR_NUMBER();
    DECLARE @errline INT            = ERROR_LINE();
    PRINT 'ERROR ' + CAST(@errnum AS NVARCHAR) + ' (línea ' + CAST(@errline AS NVARCHAR) + '): ' + @errmsg;
    THROW;
END CATCH;
GO
