using Microsoft.AspNetCore.Http;
using MyVirtualAcademy.Models;

namespace MyVirtualAcademy.Repositories
{
    /// <summary>
    /// Métodos del repositorio relacionados con contenidos, temas, exámenes,
    /// tareas, entregas y progreso del estudiante.
    /// Implementado por <see cref="RepositoryMyVirtualAcademy"/>.
    /// </summary>
    public interface IContentRepository
    {
        // ─── Contenidos ──────────────────────────────────────────────────────
        Task<Contenido> CreateContenidoAsync(int idTema, string titulo, string tipo, string urlContenido,
                                             string descripcion, int orden,
                                             DateTime? fechaEntrega = null, decimal? puntuacionMaxima = null);
        Task<bool> DeleteContenidoAsync(int idContenido);
        Task<Contenido> ObtenerContenidoPorIdAsync(int id);
        Task<bool> UsuarioTieneAccesoAsync(int idContenido, int usuarioId, bool esAdmin);
        Task RegistrarVisualizacionRecursoAsync(int idUsuario, int idContenido);
        Task<bool> ActualizarTareaAsync(int contenidoId, TareaEditViewModel model, string urlContenido = null);

        // ─── Temas ───────────────────────────────────────────────────────────
        Task<Tema> CreateTemaAsync(int idAsignatura, string nombre, int Orden);
        Task<bool> DeleteTemaAsync(int idTema);

        // ─── Exámenes ────────────────────────────────────────────────────────
        Task<Examen> CreateExamenAsync(int idContenido, int duracionMinutos, DateTime fechaPublicacionNotas,
                                       int numeroIntentos, decimal PenalizacionIncorrecta);

        // ─── Tareas / Entregas / Calificaciones ──────────────────────────────
        Task<TareaViewModel> ObtenerTareaDetalleAsync(int contenidoId, int usuarioId);
        Task<List<EntregaTareaViewModel>> ObtenerEntregasTareaAsync(int contenidoId);
        Task<int> GuardarCalificacionAsync(int idEntrega, int idEstudiante, int idContenido,
                                           decimal calificacion, string comentarios, int idProfesor);
        Task<bool> GuardarEntregaAsync(int contenidoId, int usuarioId, string comentario, IFormFile archivo);

        // ─── Progreso ───────────────────────────────────────────────────────
        Task<int?> ObtenerCursoPorContenidoAsync(int idContenido);
        Task<Inscripcion> ObtenerInscripcionAsync(int cursoId, int usuarioId);
        Task<ProgresoInscripcion> ObtenerProgresoAsync(int inscripcionId, int idContenido);
        Task RegistrarProgresoAsync(ProgresoInscripcion progreso);
        Task GuardarCambiosAsync();
    }
}
