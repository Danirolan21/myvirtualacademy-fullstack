using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyVirtualAcademy.Repositories;

namespace MyVirtualAcademy.API.Controllers
{
    [ApiController]
    [Route("api/topics")]
    [Authorize(Policy = "ProfesorUTutor")]
    public class TopicsController : ControllerBase
    {
        private readonly RepositoryMyVirtualAcademy repo;

        public TopicsController(RepositoryMyVirtualAcademy repo)
        {
            this.repo = repo;
        }

        public record CreateTopicRequest(int IdAsignatura, string Nombre, int Orden);

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateTopicRequest request)
        {
            await repo.CreateTemaAsync(request.IdAsignatura, request.Nombre, request.Orden);
            return Ok(new { message = "Tema creado correctamente" });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var ok = await repo.DeleteTemaAsync(id);
            if (!ok) return NotFound(new { message = "Módulo no encontrado" });
            return Ok(new { message = "Módulo eliminado correctamente" });
        }
    }
}
