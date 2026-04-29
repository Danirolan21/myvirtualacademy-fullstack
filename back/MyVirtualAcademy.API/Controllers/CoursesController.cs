using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyVirtualAcademy.API.Services;
using MyVirtualAcademy.Data;
using MyVirtualAcademy.Helper;
using MyVirtualAcademy.Repositories;

namespace MyVirtualAcademy.API.Controllers
{
    [ApiController]
    [Route("api/courses")]
    [Authorize]
    public class CoursesController : ControllerBase
    {
        private readonly RepositoryMyVirtualAcademy repo;
        private readonly HelperPathProvider helperPath;
        private readonly MyVirtualAcademyContext context;
        private readonly NotificationService notifService;

        public CoursesController(RepositoryMyVirtualAcademy repo, HelperPathProvider helperPath,
            MyVirtualAcademyContext context, NotificationService notifService)
        {
            this.repo = repo;
            this.helperPath = helperPath;
            this.context = context;
            this.notifService = notifService;
        }

        [HttpGet]
        [Authorize(Policy = "AdminOnly")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetAll()
        {
            var cursos = await repo.GetCursosDetallesAsync();
            return Ok(cursos);
        }

        [HttpGet("by-professor/{profesorId:int}")]
        [Authorize(Policy = "ProfesorUTutor")]
        public async Task<IActionResult> ByProfessor(int profesorId)
        {
            var cursos = await repo.GetCursosByProfesorAsync(profesorId);
            return Ok(cursos);
        }

        [HttpGet("{id:int}")]
        [Authorize(Policy = "ProfesorUTutor")]
        public async Task<IActionResult> GetById(int id)
        {
            var curso = await repo.GetDetallesCursoAsync(id);
            if (curso == null) return NotFound();

            var asignaturas = await repo.GetAsignaturasPorCursoAsync(id);
            var alumnos = await repo.GetAlumnosPorCursoAsync(id);
            var profesores = await repo.GetProfesoresAsync();

            return Ok(new
            {
                curso,
                asignaturas,
                alumnos = alumnos.Select(a => new
                {
                    a.IdUsuario,
                    a.Nombre,
                    a.Apellidos,
                    a.Email
                }),
                profesores = profesores.Select(p => new
                {
                    p.IdUsuario,
                    p.Nombre,
                    p.Apellidos
                })
            });
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        [RequestSizeLimit(10_485_760)]
        public async Task<IActionResult> Create([FromForm] CreateCourseRequest request)
        {
            string? fileName = null;
            if (request.ImagenPortada != null)
            {
                fileName = request.ImagenPortada.FileName;
                var path = helperPath.MapPath(fileName, Folders.courses);
                using var stream = new FileStream(path, FileMode.Create);
                await request.ImagenPortada.CopyToAsync(stream);
            }

            await repo.CreateCourseAsync(
                request.Nombre, request.Descripcion, request.IdProfesor,
                request.FechaInicio, request.FechaFin, request.Estado, fileName);

            return Ok(new { message = "Curso creado correctamente" });
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        [RequestSizeLimit(10_485_760)]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateCourseRequest request)
        {
            string? fileName = null;
            if (request.ImagenPortada != null)
            {
                fileName = request.ImagenPortada.FileName;
                var path = helperPath.MapPath(fileName, Folders.courses);
                using var stream = new FileStream(path, FileMode.Create);
                await request.ImagenPortada.CopyToAsync(stream);
            }

            var ok = await repo.UpdateCourseAsync(
                id, request.Nombre, request.Descripcion,
                request.IdProfesor, request.IdSuplente,
                request.FechaInicio, request.FechaFin, request.Estado, fileName);

            if (!ok) return NotFound(new { message = "Curso no encontrado" });
            return Ok(new { success = true });
        }

        [HttpGet("{id:int}/enrollments")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetEnrollments(int id)
        {
            var enrollments = await repo.GetEnrollmentsByCourseAsync(id);
            return Ok(enrollments);
        }

        [HttpGet("{id:int}/enrollments/available")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> GetAvailableStudents(int id)
        {
            var students = await repo.GetAvailableStudentsForCourseAsync(id);
            return Ok(students);
        }

        [HttpPost("{id:int}/enrollments")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Enroll(int id, [FromBody] EnrollRequest request)
        {
            var ok = await repo.EnrollStudentAsync(id, request.IdEstudiante);
            if (!ok) return BadRequest(new { message = "El estudiante ya está inscrito en este curso." });

            var curso = await context.Cursos.FirstOrDefaultAsync(c => c.IdCurso == id);
            if (curso != null)
            {
                var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                _ = notifService.NotifyEnrolledAsync(request.IdEstudiante, curso.Nombre, adminId);
            }

            return Ok(new { message = "Estudiante inscrito correctamente" });
        }

        [HttpDelete("{id:int}/enrollments/{idEstudiante:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Unenroll(int id, int idEstudiante)
        {
            var ok = await repo.UnenrollStudentAsync(id, idEstudiante);
            if (!ok) return NotFound(new { message = "Inscripción no encontrada" });
            return Ok(new { message = "Estudiante desinscrito correctamente" });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await repo.DeleteCourseAsync(id);
            if (!ok) return NotFound(new { message = "Curso no encontrado" });
            return Ok(new { message = "Curso eliminado correctamente" });
        }

        public record EnrollRequest(int IdEstudiante);

        public record CreateCourseRequest(
            string Nombre,
            string? Descripcion,
            int IdProfesor,
            DateTime FechaInicio,
            DateTime FechaFin,
            string Estado,
            IFormFile? ImagenPortada);

        public record UpdateCourseRequest(
            string Nombre,
            string? Descripcion,
            int IdProfesor,
            int? IdSuplente,
            DateTime FechaInicio,
            DateTime FechaFin,
            string Estado,
            IFormFile? ImagenPortada);
    }
}
