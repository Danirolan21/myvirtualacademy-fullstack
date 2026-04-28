using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyVirtualAcademy.Repositories;

namespace MyVirtualAcademy.API.Controllers
{
    [ApiController]
    [Route("api/calendar")]
    [Authorize]
    public class CalendarController : ControllerBase
    {
        private readonly RepositoryMyVirtualAcademy repo;

        public CalendarController(RepositoryMyVirtualAcademy repo)
        {
            this.repo = repo;
        }

        [HttpGet("events")]
        public async Task<IActionResult> GetEvents()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            var events = await repo.GetCalendarEventsAsync(userId, role);
            return Ok(events);
        }
    }
}
