using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyVirtualAcademy.Data;
using MyVirtualAcademy.Models;
using System.Security.Claims;

namespace MyVirtualAcademy.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly MyVirtualAcademyContext context;

        public NotificationsController(MyVirtualAcademyContext context)
        {
            this.context = context;
        }

        private int CurrentUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = CurrentUserId();
            var items = await context.Notificaciones
                .Where(n => n.IdUsuario == userId)
                .OrderByDescending(n => n.FechaCreacion)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new
                {
                    n.IdNotificacion,
                    n.Tipo,
                    n.Titulo,
                    n.Mensaje,
                    n.Leida,
                    n.FechaCreacion
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount()
        {
            var userId = CurrentUserId();
            var count = await context.Notificaciones
                .CountAsync(n => n.IdUsuario == userId && !n.Leida);
            return Ok(new { count });
        }

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = CurrentUserId();
            var notif = await context.Notificaciones
                .FirstOrDefaultAsync(n => n.IdNotificacion == id && n.IdUsuario == userId);
            if (notif == null) return NotFound();
            notif.Leida = true;
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = CurrentUserId();
            await context.Notificaciones
                .Where(n => n.IdUsuario == userId && !n.Leida)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.Leida, true));
            return NoContent();
        }
    }
}
