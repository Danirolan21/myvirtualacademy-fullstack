# Pending Features

## Comentarios en contenidos

**Scope**: Omitted from initial migration.

**Where it would appear**: VideoDetailView, DocumentDetailView, LinkDetailView, TaskDetailView — after the main content area, a comment thread section allowing students and professors to post text messages attached to a specific content item.

**Backend prerequisites**:
- New table `Comentarios` (idComentario PK, idContenido FK, idUsuario FK, texto, fechaCreacion)
- `GET /api/content/{id}/comments` — list comments for a content item
- `POST /api/content/{id}/comments` — post a new comment (authenticated)

**Frontend shape**:
- `CommentThread.vue` component receiving `idContenido` prop
- Thread displays avatar, name, date, text; newest-first or oldest-first
- Textarea + submit button at the bottom; optimistic append on success
