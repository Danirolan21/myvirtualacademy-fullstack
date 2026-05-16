using Microsoft.EntityFrameworkCore;
using MyVirtualAcademy.Data;
using MyVirtualAcademy.Models;

namespace MyVirtualAcademy.API.Services
{
    public class NotificationService
    {
        private readonly MyVirtualAcademyContext context;

        public NotificationService(MyVirtualAcademyContext context)
        {
            this.context = context;
        }

        public async Task NotifyTaskSubmittedAsync(int contenidoId, int estudianteId, string nombreEstudiante, string tituloContenido)
        {
            var profesores = await context.Contenidos
                .Where(c => c.IdContenido == contenidoId)
                .Join(context.Temas, c => c.IdTema, t => t.IdTema, (c, t) => t)
                .Join(context.ProfesoresAsignaturas, t => t.IdAsignatura, pa => pa.IdAsignatura, (t, pa) => pa.IdProfesor)
                .Distinct()
                .ToListAsync();

            var notifs = profesores.Select(profesorId => new Notificacion
            {
                IdUsuario = profesorId,
                Tipo = "entrega",
                Titulo = "Nueva entrega recibida",
                Mensaje = $"{nombreEstudiante} ha entregado la tarea \"{tituloContenido}\".",
                Leida = false,
                FechaCreacion = DateTime.UtcNow,
                EnviadoPor = estudianteId,
                IdReferencia = contenidoId
            });

            context.Notificaciones.AddRange(notifs);
            await context.SaveChangesAsync();
        }

        public async Task NotifyTaskGradedAsync(int contenidoId, int estudianteId, string tituloContenido, decimal calificacion, int profesorId)
        {
            context.Notificaciones.Add(new Notificacion
            {
                IdUsuario = estudianteId,
                Tipo = "calificacion",
                Titulo = "Tarea calificada",
                Mensaje = $"Tu entrega de \"{tituloContenido}\" ha sido calificada con {calificacion:F1}.",
                Leida = false,
                FechaCreacion = DateTime.UtcNow,
                EnviadoPor = profesorId,
                IdReferencia = contenidoId
            });
            await context.SaveChangesAsync();
        }

        public async Task NotifyEnrolledAsync(int estudianteId, string nombreCurso, int? adminId = null)
        {
            context.Notificaciones.Add(new Notificacion
            {
                IdUsuario = estudianteId,
                Tipo = "inscripcion",
                Titulo = "Inscripción confirmada",
                Mensaje = $"Has sido inscrito en el curso \"{nombreCurso}\".",
                Leida = false,
                FechaCreacion = DateTime.UtcNow,
                EnviadoPor = adminId
            });
            await context.SaveChangesAsync();
        }

        public async Task NotifyNewContentAsync(int contenidoId, int profesorId)
        {
            var info = await context.Contenidos
                .Where(c => c.IdContenido == contenidoId)
                .Select(c => new { c.Titulo, c.Tema.IdAsignatura })
                .FirstOrDefaultAsync();
            if (info == null) return;

            var estudianteIds = await context.Inscripciones
                .Where(i => i.Estado == "Activo" &&
                    context.Asignaturas
                        .Where(a => a.IdAsignatura == info.IdAsignatura)
                        .Select(a => a.IdCurso)
                        .Contains(i.IdCurso))
                .Select(i => i.IdEstudiante)
                .Distinct()
                .ToListAsync();

            if (!estudianteIds.Any()) return;

            context.Notificaciones.AddRange(estudianteIds.Select(id => new Notificacion
            {
                IdUsuario = id,
                Tipo = "contenido",
                Titulo = "Nuevo contenido disponible",
                Mensaje = $"Se ha publicado nuevo contenido: \"{info.Titulo}\".",
                Leida = false,
                FechaCreacion = DateTime.UtcNow,
                EnviadoPor = profesorId,
                IdReferencia = contenidoId
            }));
            await context.SaveChangesAsync();
        }
    }
}
