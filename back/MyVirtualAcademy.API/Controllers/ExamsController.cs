using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyVirtualAcademy.Repositories;

namespace MyVirtualAcademy.API.Controllers
{
    // Stub — funcionalidad de exámenes pendiente de implementación completa
    [ApiController]
    [Route("api/exams")]
    [Authorize(Policy = "ProfesorUTutor")]
    public class ExamsController : ControllerBase
    {
        private readonly RepositoryMyVirtualAcademy repo;

        public ExamsController(RepositoryMyVirtualAcademy repo)
        {
            this.repo = repo;
        }

        public record CreateExamRequest(
            int IdContenido,
            int DuracionMinutos,
            DateTime FechaPublicacionNotas,
            int IntentosMaximos,
            decimal PenalizacionIncorrecta);

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateExamRequest request)
        {
            var examen = await repo.CreateExamenAsync(
                request.IdContenido, request.DuracionMinutos,
                request.FechaPublicacionNotas, request.IntentosMaximos,
                request.PenalizacionIncorrecta);
            return StatusCode(StatusCodes.Status201Created,
                new { idContenido = examen.IdContenido, message = "Examen creado correctamente" });
        }
    }
}
