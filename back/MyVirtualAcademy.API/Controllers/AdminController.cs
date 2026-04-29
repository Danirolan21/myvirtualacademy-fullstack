using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyVirtualAcademy.Data;

namespace MyVirtualAcademy.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : ControllerBase
    {
        private readonly MyVirtualAcademyContext context;

        public AdminController(MyVirtualAcademyContext context)
        {
            this.context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalCursos = await context.Cursos.CountAsync();
            var cursosActivos = await context.Cursos.CountAsync(c => c.Estado == "Activo");

            var totalUsuarios = await context.Usuarios.CountAsync();

            var rolEstudiante = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == "Estudiante");
            var rolProfesor = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == "Profesor");
            var rolTutor = await context.Roles.FirstOrDefaultAsync(r => r.Nombre == "Tutor");

            var totalEstudiantes = rolEstudiante != null
                ? await context.UsuariosRoles.CountAsync(ur => ur.IdRol == rolEstudiante.IdRol)
                : 0;
            var totalProfesores = (rolProfesor != null || rolTutor != null)
                ? await context.UsuariosRoles.CountAsync(ur =>
                    (rolProfesor != null && ur.IdRol == rolProfesor.IdRol) ||
                    (rolTutor != null && ur.IdRol == rolTutor.IdRol))
                : 0;

            var totalInscripciones = await context.Inscripciones.CountAsync();
            var entregasPendientes = await context.EntregasTareas.CountAsync(e => e.Estado == "Pendiente");

            var actividadReciente = await context.EntregasTareas
                .Include(e => e.Contenido)
                .Include(e => e.Usuario)
                .OrderByDescending(e => e.FechaEntrega)
                .Take(5)
                .Select(e => new
                {
                    idEntrega = e.IdEntrega,
                    nombreEstudiante = e.Usuario.Nombre + " " + e.Usuario.Apellidos,
                    tituloContenido = e.Contenido.Titulo,
                    fechaEntrega = e.FechaEntrega,
                    estado = e.Estado
                })
                .ToListAsync();

            return Ok(new
            {
                totalCursos,
                cursosActivos,
                totalUsuarios,
                totalEstudiantes,
                totalProfesores,
                totalInscripciones,
                entregasPendientes,
                actividadReciente
            });
        }
    }
}
