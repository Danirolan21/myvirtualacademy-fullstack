using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyVirtualAcademy.Repositories;

namespace MyVirtualAcademy.API.Controllers
{
    [ApiController]
    [Route("api/tasks")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly RepositoryMyVirtualAcademy repo;

        public TasksController(RepositoryMyVirtualAcademy repo)
        {
            this.repo = repo;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var tarea = await repo.ObtenerTareaDetalleAsync(id, userId);
            if (tarea == null) return NotFound();

            if (User.IsInRole("Profesor") || User.IsInRole("Administrador")
                || User.HasClaim("IsAdmin", "true"))
            {
                tarea.Entregas = await repo.ObtenerEntregasTareaAsync(id);
            }

            return Ok(tarea);
        }

        [HttpGet("{id:int}/submissions")]
        [Authorize(Policy = "ProfesorUTutor")]
        public async Task<IActionResult> GetSubmissions(int id)
        {
            var entregas = await repo.ObtenerEntregasTareaAsync(id);
            return Ok(entregas);
        }

        [HttpPost("{id:int}/submit")]
        [RequestSizeLimit(10_485_760)]
        public async Task<IActionResult> Submit(int id,
            [FromForm] string? comentarioEstudiante,
            IFormFile? archivoEntrega)
        {
            if (archivoEntrega == null)
                return BadRequest(new { message = "Debes adjuntar un archivo." });

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await repo.GuardarEntregaAsync(id, userId, comentarioEstudiante ?? string.Empty, archivoEntrega);

            if (!ok)
                return BadRequest(new { message = "Error al procesar la entrega. Comprueba el tamaño del archivo (máx. 10 MB)." });

            return Ok(new { message = "Entrega realizada correctamente" });
        }

        [HttpPost("{id:int}/grade")]
        [Authorize(Policy = "ProfesorUTutor")]
        public async Task<IActionResult> Grade(int id, [FromBody] GradeRequest request)
        {
            var profesorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await repo.GuardarCalificacionAsync(
                request.IdEntrega, request.IdEstudiante, id,
                request.Calificacion, request.Comentarios ?? string.Empty, profesorId);

            return Ok(new { message = "Calificación guardada correctamente" });
        }

        public record GradeRequest(
            int IdEntrega,
            int IdEstudiante,
            decimal Calificacion,
            string? Comentarios);
    }
}
