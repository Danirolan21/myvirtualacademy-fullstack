-- =============================================================================
-- 005_identity_cleanup.sql
-- Elimina las 8 tablas *_OLD creadas por 005_identity_migration.sql.
-- Ejecutar SOLO tras:
--   1) Aplicar 005_identity_migration.sql
--   2) Verificar con 005_identity_verify.sql que TODOS los checks están en OK
--   3) Validar el funcionamiento de la app en producción durante un periodo
--      razonable (no hay forma de rollback una vez ejecutado esto)
-- =============================================================================

USE [MyVirtualAcademy];
GO

PRINT '--- Eliminando tablas *_OLD ---';

DROP TABLE IF EXISTS [dbo].[Usuarios_OLD];
DROP TABLE IF EXISTS [dbo].[Cursos_OLD];
DROP TABLE IF EXISTS [dbo].[Asignaturas_OLD];
DROP TABLE IF EXISTS [dbo].[Temas_OLD];
DROP TABLE IF EXISTS [dbo].[Contenidos_OLD];
DROP TABLE IF EXISTS [dbo].[Historial_Calificaciones_OLD];
DROP TABLE IF EXISTS [dbo].[Comentarios_Calificaciones_OLD];
DROP TABLE IF EXISTS [dbo].[Entregas_Tareas_OLD];

PRINT 'Cleanup completado. Las tablas *_OLD ya no existen.';
GO
