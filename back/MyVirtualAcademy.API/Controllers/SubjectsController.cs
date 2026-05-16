using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyVirtualAcademy.Repositories;

namespace MyVirtualAcademy.API.Controllers
{
    [ApiController]
    [Route("api/subjects")]
    [Authorize]
    public class SubjectsController : ControllerBase
    {
        private readonly RepositoryMyVirtualAcademy repo;

        public SubjectsController(RepositoryMyVirtualAcademy repo)
        {
            this.repo = repo;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var detalle = await repo.GetDetallesAsignatura(id);
            if (detalle == null) return NotFound();

            var curso = await repo.GetCursoPorAsignaturaAsync(id);
            return Ok(new { detalle, idCurso = curso?.IdCurso });
        }

        [HttpGet("by-professor/{profesorId:int}")]
        public async Task<IActionResult> ByProfessor(int profesorId)
        {
            var asignaturas = await repo.GetAsignaturasByProfesorAsync(profesorId);
            return Ok(asignaturas);
        }

        [HttpGet("by-student/{estudianteId:int}")]
        public async Task<IActionResult> ByStudent(int estudianteId)
        {
            var asignaturas = await repo.GetAsignaturasByUserAsync(estudianteId);
            return Ok(asignaturas);
        }

        [HttpPost]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Create([FromBody] CreateSubjectRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreAsignatura))
                return BadRequest(new { message = "El nombre de la asignatura es obligatorio." });

            var asignatura = await repo.CreateAsignaturaAsync(
                request.IdCurso,
                request.NombreAsignatura.Trim(),
                request.IdProfesor);

            return StatusCode(StatusCodes.Status201Created, new
            {
                asignatura.IdAsignatura,
                asignatura.Nombre,
                asignatura.IdCurso,
                message = "Asignatura creada correctamente"
            });
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSubjectRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreAsignatura))
                return BadRequest(new { message = "El nombre de la asignatura es obligatorio." });

            var updated = await repo.UpdateAsignaturaAsync(id, request.NombreAsignatura.Trim());
            if (updated == null) return NotFound();

            return Ok(new { updated.IdAsignatura, updated.Nombre });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await repo.DeleteAsignaturaAsync(id);
            if (!ok) return NotFound();
            return Ok(new { message = "Asignatura eliminada." });
        }

        [HttpPost("{id:int}/profesores")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> AddProfesor(int id, [FromBody] AddProfesorRequest request)
        {
            var (added, conflict) = await repo.AddProfesorAsignaturaAsync(id, request.IdProfesor);
            if (conflict) return Conflict(new { message = "El profesor ya está asignado a esta asignatura." });
            if (!added) return NotFound();
            return Ok(new { message = "Profesor asignado." });
        }

        [HttpDelete("{id:int}/profesores/{idProfesor:int}")]
        [Authorize(Policy = "AdminOnly")]
        public async Task<IActionResult> RemoveProfesor(int id, int idProfesor)
        {
            var ok = await repo.RemoveProfesorAsignaturaAsync(id, idProfesor);
            if (!ok) return NotFound();
            return Ok(new { message = "Profesor desasignado." });
        }

        public record CreateSubjectRequest(int IdCurso, string NombreAsignatura, int? IdProfesor);
        public record UpdateSubjectRequest(string NombreAsignatura);
        public record AddProfesorRequest(int IdProfesor);
    }
}
