using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyVirtualAcademy.Helper;
using MyVirtualAcademy.Repositories;

namespace MyVirtualAcademy.API.Controllers
{
    [ApiController]
    [Route("api/courses")]
    [Authorize(Policy = "AdminOnly")]
    public class CoursesController : ControllerBase
    {
        private readonly RepositoryMyVirtualAcademy repo;
        private readonly HelperPathProvider helperPath;

        public CoursesController(RepositoryMyVirtualAcademy repo, HelperPathProvider helperPath)
        {
            this.repo = repo;
            this.helperPath = helperPath;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var cursos = await repo.GetCursosDetallesAsync();
            return Ok(cursos);
        }

        [HttpGet("{id:int}")]
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
