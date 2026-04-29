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
                EnviadoPor = estudianteId
            });

            context.Notificaciones.AddRange(notifs);
            await context.SaveChangesAsync();
        }

        public async Task NotifyTaskGradedAsync(int estudianteId, string tituloContenido, decimal calificacion, int profesorId)
        {
            context.Notificaciones.Add(new Notificacion
            {
                IdUsuario = estudianteId,
                Tipo = "calificacion",
                Titulo = "Tarea calificada",
                Mensaje = $"Tu entrega de \"{tituloContenido}\" ha sido calificada con {calificacion:F1}.",
                Leida = false,
                FechaCreacion = DateTime.UtcNow,
                EnviadoPor = profesorId
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
    }
}
