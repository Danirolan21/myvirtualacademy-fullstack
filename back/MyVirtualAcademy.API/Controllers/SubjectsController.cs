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
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
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
    }
}
