using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyVirtualAcademy.Helper;
using MyVirtualAcademy.Models;
using MyVirtualAcademy.Repositories;

namespace MyVirtualAcademy.API.Controllers
{
    [ApiController]
    [Route("api/content")]
    [Authorize]
    public class ContentController : ControllerBase
    {
        private readonly RepositoryMyVirtualAcademy repo;
        private readonly HelperPathProvider helperPath;

        public ContentController(RepositoryMyVirtualAcademy repo, HelperPathProvider helperPath)
        {
            this.repo = repo;
            this.helperPath = helperPath;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var contenido = await repo.ObtenerContenidoPorIdAsync(id);
            if (contenido == null) return NotFound();
            return Ok(contenido);
        }

        [HttpGet("{id:int}/access")]
        public async Task<IActionResult> CheckAccess(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var isAdmin = User.HasClaim("IsAdmin", "true");
            var hasAccess = await repo.UsuarioTieneAccesoAsync(id, userId, isAdmin);
            return Ok(new { hasAccess });
        }

        [HttpPost("{id:int}/view")]
        public async Task<IActionResult> RegisterView(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            await repo.RegistrarVisualizacionRecursoAsync(userId, id);
            return NoContent();
        }

        [HttpPost]
        [Authorize(Policy = "ProfesorUTutor")]
        [RequestSizeLimit(10_485_760)]
        public async Task<IActionResult> Create([FromForm] CreateContentRequest request)
        {
            string? fileName = null;
            if (request.ArchivoContenido != null)
            {
                fileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.ArchivoContenido.FileName)}";
                var path = helperPath.MapPath(fileName, Folders.contents);
                using var stream = new FileStream(path, FileMode.Create);
                await request.ArchivoContenido.CopyToAsync(stream);
            }

            var contenido = await repo.CreateContenidoAsync(
                request.IdTema, request.Titulo, request.Tipo,
                fileName ?? request.UrlContenido ?? string.Empty,
                request.Descripcion ?? string.Empty,
                request.Orden,
                request.FechaEntrega,
                request.PuntuacionMaxima);

            if ((request.Tipo == "Examen" || request.Tipo == "Quiz")
                && request.DuracionMinutos.HasValue
                && request.FechaPublicacionNotas.HasValue)
            {
                await repo.CreateExamenAsync(
                    contenido.IdContenido,
                    request.DuracionMinutos.Value,
                    request.FechaPublicacionNotas.Value,
                    request.IntentosMaximos ?? 1,
                    request.PenalizacionIncorrecta ?? 0);
            }

            return Ok(new { contenido.IdContenido, idAsignatura = contenido.Tema.IdAsignatura });
        }

        [HttpPut("{id:int}")]
        [Authorize(Policy = "ProfesorUTutor")]
        [RequestSizeLimit(10_485_760)]
        public async Task<IActionResult> Update(int id, [FromForm] UpdateContentRequest request)
        {
            string? urlContenido = null;
            if (request.ArchivoContenido != null && request.ArchivoContenido.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.ArchivoContenido.FileName)}";
                var path = helperPath.MapPath(fileName, Folders.contents);
                using var stream = new FileStream(path, FileMode.Create);
                await request.ArchivoContenido.CopyToAsync(stream);
                urlContenido = fileName;
            }

            var model = new TareaEditViewModel
            {
                Titulo = request.Titulo,
                Descripcion = request.Descripcion ?? string.Empty,
                FechaEntrega = request.FechaEntrega ?? DateTime.Now.AddDays(7),
                PuntuacionMaxima = request.PuntuacionMaxima ?? 10
            };

            var ok = await repo.ActualizarTareaAsync(id, model, urlContenido);
            if (!ok) return NotFound();
            return Ok(new { message = "Contenido actualizado" });
        }

        public record CreateContentRequest(
            int IdTema,
            string Titulo,
            string Tipo,
            string? UrlContenido,
            string? Descripcion,
            int Orden,
            DateTime? FechaEntrega,
            decimal? PuntuacionMaxima,
            IFormFile? ArchivoContenido,
            int? DuracionMinutos,
            DateTime? FechaPublicacionNotas,
            int? IntentosMaximos,
            decimal? PenalizacionIncorrecta);

        public record UpdateContentRequest(
            string Titulo,
            string? Descripcion,
            DateTime? FechaEntrega,
            decimal? PuntuacionMaxima,
            IFormFile? ArchivoContenido);
    }
}
