using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyVirtualAcademy.Data;
using MyVirtualAcademy.Helper;

namespace MyVirtualAcademy.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : ControllerBase
    {
        private readonly MyVirtualAcademyContext context;
        private readonly HelperPathProvider helperPath;

        public AdminController(MyVirtualAcademyContext context, HelperPathProvider helperPath)
        {
            this.context = context;
            this.helperPath = helperPath;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalCursos = await context.Cursos.CountAsync();
            var cursosActivos = await context.Cursos.CountAsync(c => c.Estado == "Activo");

            var totalUsuarios = await context.Usuarios.CountAsync();

            var rolEstudiante = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == "Estudiante");
            var rolProfesor = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == "Profesor");
            var rolTutor = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == "Tutor");

            var totalEstudiantes = rolEstudiante != null
                ? await context.UsuariosRoles.CountAsync(ur => ur.IdRol == rolEstudiante.IdRol)
                : 0;
            var totalProfesores = (rolProfesor != null || rolTutor != null)
                ? await context.UsuariosRoles.CountAsync(ur =>
                    (rolProfesor != null && ur.IdRol == rolProfesor.IdRol) ||
                    (rolTutor != null && ur.IdRol == rolTutor.IdRol))
                : 0;

            var totalInscripciones = await context.Inscripciones.CountAsync();
            var entregasPendientes = await context.EntregasTareas.CountAsync(e => e.Estado == "Pendiente");

            var actividadReciente = await context.EntregasTareas
                .Include(e => e.Contenido)
                .Include(e => e.Usuario)
                .OrderByDescending(e => e.FechaEntrega)
                .Take(5)
                .Select(e => new
                {
                    idEntrega = e.IdEntrega,
                    nombreEstudiante = e.Usuario.Nombre + " " + e.Usuario.Apellidos,
                    tituloContenido = e.Contenido.Titulo,
                    fechaEntrega = e.FechaEntrega,
                    estado = e.Estado
                })
                .ToListAsync();

            return Ok(new
            {
                totalCursos,
                cursosActivos,
                totalUsuarios,
                totalEstudiantes,
                totalProfesores,
                totalInscripciones,
                entregasPendientes,
                actividadReciente
            });
        }

        // TEMPORAL: endpoint de auditoría — ELIMINAR cuando se complete
        // la limpieza. Ver issue/ticket de auditoría de huérfanos.
        [HttpGet("audit/orphan-contents")]
        public async Task<IActionResult> AuditOrphanContents()
        {
            var contentsFolder = helperPath.MapPath(string.Empty, Folders.contents).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // --- Contenidos con archivo en disco que no existe ---
            var candidateContents = await context.Contenidos
                .Include(c => c.Tema)
                    .ThenInclude(t => t.Asignatura)
                        .ThenInclude(a => a.Curso)
                .Where(c =>
                    !string.IsNullOrEmpty(c.URLContenido) &&
                    !c.URLContenido.StartsWith("http://") &&
                    !c.URLContenido.StartsWith("https://"))
                .ToListAsync();

            var orphanContents = candidateContents
                .Where(c => !System.IO.File.Exists(Path.Combine(contentsFolder, c.URLContenido)))
                .Select(c => new
                {
                    idContenido = c.IdContenido,
                    titulo = c.Titulo,
                    tipo = c.Tipo,
                    urlContenido = c.URLContenido,
                    idTema = c.IdTema,
                    nombreTema = c.Tema?.Nombre ?? "",
                    idAsignatura = c.Tema?.IdAsignatura ?? 0,
                    nombreAsignatura = c.Tema?.Asignatura?.Nombre ?? "",
                    idCurso = c.Tema?.Asignatura?.IdCurso ?? 0,
                    nombreCurso = c.Tema?.Asignatura?.Curso?.Nombre ?? ""
                })
                .ToList();

            // --- Entregas con archivo en disco que no existe ---
            var candidateSubmissions = await context.EntregasTareas
                .Include(e => e.Contenido)
                .Include(e => e.Usuario)
                .Where(e => !string.IsNullOrEmpty(e.URLEntrega) &&
                            !e.URLEntrega.StartsWith("http://") &&
                            !e.URLEntrega.StartsWith("https://"))
                .ToListAsync();

            var orphanSubmissions = candidateSubmissions
                .Where(e => !System.IO.File.Exists(Path.Combine(contentsFolder, e.URLEntrega)))
                .Select(e => new
                {
                    idEntrega = e.IdEntrega,
                    urlEntrega = e.URLEntrega,
                    idEstudiante = e.IdEstudiante,
                    nombreEstudiante = e.Usuario != null
                        ? $"{e.Usuario.Nombre} {e.Usuario.Apellidos}"
                        : $"ID {e.IdEstudiante}",
                    idContenido = e.IdContenido,
                    tituloContenido = e.Contenido?.Titulo ?? ""
                })
                .ToList();

            // --- Archivos en disco sin registro en BD ---
            var referencedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in candidateContents) referencedFiles.Add(c.URLContenido);
            foreach (var e in candidateSubmissions) referencedFiles.Add(e.URLEntrega);

            var allFiles = Directory.Exists(contentsFolder)
                ? Directory.GetFiles(contentsFolder).Select(f => new FileInfo(f)).ToList()
                : new List<FileInfo>();

            var orphanFiles = allFiles
                .Where(f => !referencedFiles.Contains(f.Name))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .Take(100)
                .Select(f => new
                {
                    fileName = f.Name,
                    sizeKB = (int)(f.Length / 1024),
                    lastModified = f.LastWriteTimeUtc.ToString("O")
                })
                .ToList();

            return Ok(new
            {
                orphanContents,
                orphanSubmissions,
                orphanFiles,
                summary = new
                {
                    totalContentsChecked = candidateContents.Count,
                    orphanContentsCount = orphanContents.Count,
                    totalFilesInFolder = allFiles.Count,
                    orphanFilesCount = orphanFiles.Count
                }
            });
        }
    }
}
