-- =============================================================================
-- 006_add_id_referencia_notificaciones.sql
-- Añade columna ID_Referencia (INT NULL) a la tabla Notificaciones para que
-- cada notificación pueda guardar el ID del recurso al que apunta
-- (IdContenido para tipos 'contenido', 'entrega' y 'calificacion').
--
-- Sin FK constraint a propósito: si el contenido se elimina, las notificaciones
-- huérfanas persisten (preserva historial); al click el frontend caerá en
-- NotFoundView de forma natural.
--
-- Aplicado en Azure SQL el 2026-05-16.
-- =============================================================================

ALTER TABLE Notificaciones ADD ID_Referencia INT NULL;
