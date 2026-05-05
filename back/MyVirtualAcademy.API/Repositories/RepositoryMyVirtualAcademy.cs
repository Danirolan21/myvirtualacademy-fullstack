using Microsoft.EntityFrameworkCore;
using MyVirtualAcademy.Data;
using MyVirtualAcademy.Helper;
using MyVirtualAcademy.Models;

namespace MyVirtualAcademy.Repositories
{
    public class RepositoryMyVirtualAcademy
    {
        private MyVirtualAcademyContext context;
        private HelperPathProvider helperPath;

        public RepositoryMyVirtualAcademy(MyVirtualAcademyContext context, HelperPathProvider helperPath)
        {
            this.context = context;
            this.helperPath = helperPath;
        }

        #region MANAGED METHODS
        private async Task<int> GetMaxIdUserAsync()
        {
            if (this.context.Usuarios.Count() == 0)
            {
                return 1;
            }
            else
            {
                return await this.context.Usuarios.MaxAsync
                    (x => x.IdUsuario) + 1;
            }
        }

        public async Task Register(string nombre, string apellidos,
            string email, string password, int idRol)
        {
            Usuario user = new Usuario();
            user.IdUsuario = await GetMaxIdUserAsync();
            user.Nombre = nombre;
            user.Apellidos = apellidos;
            user.Email = email;
            user.Salt = HelperCryptography.GenerateSalt();
            user.Password_Hash = HelperCryptography.EncryptPassword(password, user.Salt);
            user.Password = password;
            user.FechaRegistro = DateTime.Now;
            user.FotoPerfil = "ProfileImage_Default.jpg";

            this.context.Usuarios.Add(user);
            await this.context.SaveChangesAsync();

            await AsignarRolUsuarioAsync(user.IdUsuario, idRol);
        }

        public async Task<Usuario> LogInUserAsync(string email, string password)
        {
            var consulta = from datos in this.context.Usuarios
                           where datos.Email == email
                           select datos;
            Usuario user = await consulta.FirstOrDefaultAsync();
            if (user == null)
            {
                return null;
            }
            else
            {
                string salt = user.Salt;
                byte[] temp =
                    HelperCryptography.EncryptPassword(password, salt);
                byte[] passBytes = user.Password_Hash;
                bool response =
                    HelperCryptography.CompararArrays(temp, passBytes);
                if (response == true)
                {
                    return user;
                }
                else
                {
                    return null;
                }
            }
        }

        public async Task<Usuario?> FindUserByEmailAsync(string email)
        {
            return await this.context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task MigratePasswordToBCryptAsync(int idUsuario, string plainPassword)
        {
            var user = await this.context.Usuarios.FindAsync(idUsuario);
            if (user == null) return;
            user.PassBCrypt = HelperCryptography.HashPasswordBCrypt(plainPassword);
            user.MigratedToBCrypt = true;
            user.Password = string.Empty;
            await this.context.SaveChangesAsync();
        }

        public async Task<Usuario> FindUserAsync(int idUsuario)
        {
            var consulta = from datos in this.context.Usuarios
                           where datos.IdUsuario == idUsuario
                           select datos;
            return await consulta.FirstOrDefaultAsync();
        }

        public async Task<string> GetUserRoleAsync(int idUsuario)
        {
            var consulta = from v in this.context.VistaUsuariosConRoles
                           where v.IdUsuario == idUsuario
                           select v.Rol;

            return await consulta.FirstOrDefaultAsync();
        }

        public async Task AsignarRolUsuarioAsync(int idUsuario, int idRol)
        {
            UsuarioRol usuarioRol = new UsuarioRol
            {
                IdUsuario = idUsuario,
                IdRol = idRol
            };

            this.context.UsuariosRoles.Add(usuarioRol);
            await this.context.SaveChangesAsync();
        }

        public async Task<List<Rol>> GetRolesAsync()
        {
            return await this.context.Roles.ToListAsync();
        }

        public async Task<Usuario> 
            UpdateUserAsync(int idUsuario, string nombre, string apellidos
            , string? fotoPerfil, string telefono)
        {
            Usuario user = await this.FindUserAsync(idUsuario);
            user.Nombre = nombre;
            user.Apellidos = apellidos;
            user.FotoPerfil = fotoPerfil ?? user.FotoPerfil;
            user.Telefono = telefono;
            await this.context.SaveChangesAsync();

            return user;
        }

        public async Task UpdateLastAccessAsync(int idUsuario)
        {
            Usuario user = await this.context.Usuarios.FindAsync(idUsuario);
            if (user != null)
            {
                user.UltimoAcceso = DateTime.Now;
                await this.context.SaveChangesAsync();
            }
        }
        #endregion

        #region AREA PERSONAL ADMIN

        public async Task<List<object>> GetAllUsersWithRolesAsync()
        {
            var rows = await this.context.VistaUsuariosConRoles.ToListAsync();
            return rows
                .GroupBy(u => u.IdUsuario)
                .Select(g => (object)new
                {
                    idUsuario = g.Key,
                    nombre    = g.First().Nombre,
                    apellidos = g.First().Apellidos,
                    email     = g.First().Email,
                    rol       = string.Join(", ", g.Select(u => u.Rol).Distinct()),
                    activo    = g.First().Activo
                })
                .ToList();
        }

        public async Task<(bool success, string? error)> UpdatePasswordAsync(
            int idUsuario, string currentPassword, string newPassword)
        {
            var user = await this.context.Usuarios.FindAsync(idUsuario);
            if (user == null) return (false, "Usuario no encontrado");

            bool valid;
            if (user.MigratedToBCrypt)
                valid = HelperCryptography.VerifyPasswordBCrypt(currentPassword, user.PassBCrypt!);
            else
            {
                var hash = HelperCryptography.EncryptPassword(currentPassword, user.Salt);
                valid = HelperCryptography.CompararArrays(hash, user.Password_Hash);
            }

            if (!valid) return (false, "La contraseña actual no es correcta");

            user.PassBCrypt = HelperCryptography.HashPasswordBCrypt(newPassword);
            user.MigratedToBCrypt = true;
            user.Password = string.Empty;
            await this.context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> ToggleUserActiveAsync(int idUsuario)
        {
            var user = await this.context.Usuarios.FindAsync(idUsuario);
            if (user == null) return false;
            user.Activo = !user.Activo;
            await this.context.SaveChangesAsync();
            return true;
        }

        public async Task<List<object>> GetEnrollmentsByCourseAsync(int idCurso)
        {
            var enrollments = await this.context.Inscripciones
                .Where(i => i.IdCurso == idCurso && i.Estado == "Activo")
                .Include(i => i.Estudiante)
                .Select(i => new
                {
                    i.IdInscripcion,
                    i.IdEstudiante,
                    NombreEstudiante = i.Estudiante.Nombre + " " + i.Estudiante.Apellidos,
                    i.Estudiante.Email,
                    i.FechaInscripcion,
                    i.PorcentajeCompletado,
                    i.RolInscrito
                })
                .ToListAsync();
            return enrollments.Cast<object>().ToList();
        }

        public async Task<List<VistaUsuariosConRoles>> GetAvailableStudentsForCourseAsync(int idCurso)
        {
            var enrolledIds = await this.context.Inscripciones
                .Where(i => i.IdCurso == idCurso && i.Estado == "Activo")
                .Select(i => i.IdEstudiante)
                .ToListAsync();

            return await this.context.VistaUsuariosConRoles
                .Where(u => u.Rol == "Estudiante" && !enrolledIds.Contains(u.IdUsuario))
                .ToListAsync();
        }

        public async Task<bool> EnrollStudentAsync(int idCurso, int idEstudiante)
        {
            var existing = await this.context.Inscripciones
                .FirstOrDefaultAsync(i => i.IdCurso == idCurso && i.IdEstudiante == idEstudiante);
            if (existing != null)
            {
                if (existing.Estado == "Activo") return false;
                existing.Estado = "Activo";
                await this.context.SaveChangesAsync();
                return true;
            }

            int newId = await this.context.Inscripciones.AnyAsync()
                ? await this.context.Inscripciones.MaxAsync(i => i.IdInscripcion) + 1
                : 1;

            this.context.Inscripciones.Add(new Inscripcion
            {
                IdInscripcion = newId,
                IdCurso = idCurso,
                IdEstudiante = idEstudiante,
                FechaInscripcion = DateTime.Now,
                Estado = "Activo",
                RolInscrito = "Estudiante",
                PorcentajeCompletado = 0
            });
            await this.context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UnenrollStudentAsync(int idCurso, int idEstudiante)
        {
            var inscripcion = await this.context.Inscripciones
                .FirstOrDefaultAsync(i => i.IdCurso == idCurso && i.IdEstudiante == idEstudiante && i.Estado == "Activo");
            if (inscripcion == null) return false;
            inscripcion.Estado = "Inactivo";
            await this.context.SaveChangesAsync();
            return true;
        }

        public async Task<List<VistaCursosDetalles>> GetCursosDetallesAsync()
        {
            return await this.context.VistaCursosDetalles
                .Where(c => c.Estado != "Eliminado")
                .ToListAsync();
        }

        public async Task<VistaCursosDetalles> GetDetallesCursoAsync(int idCurso)
        {
            return await this.context.VistaCursosDetalles
                .FirstOrDefaultAsync(c => c.IdCurso == idCurso && c.Estado != "Eliminado");
        }

        public async Task<bool> DeleteCourseAsync(int idCurso)
        {
            var curso = await this.context.Cursos.FindAsync(idCurso);
            if (curso == null) return false;
            curso.Estado = "Eliminado";
            await this.context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Asignatura>> GetAsignaturasPorCursoAsync(int idCurso)
        {
            return await this.context.Asignaturas
                .Where(a => a.IdCurso == idCurso)
                .ToListAsync();
        }

        private async Task<int> GetMaxIdAsignaturaAsync()
        {
            if (!this.context.Asignaturas.Any()) return 1;
            return await this.context.Asignaturas.MaxAsync(a => a.IdAsignatura) + 1;
        }

        public async Task<Asignatura> CreateAsignaturaAsync(int idCurso, string nombre, int? idProfesor = null)
        {
            var asignatura = new Asignatura
            {
                IdAsignatura = await GetMaxIdAsignaturaAsync(),
                IdCurso = idCurso,
                Nombre = nombre
            };
            this.context.Asignaturas.Add(asignatura);

            if (idProfesor.HasValue)
            {
                this.context.ProfesoresAsignaturas.Add(new ProfesorAsignatura
                {
                    IdAsignatura = asignatura.IdAsignatura,
                    IdProfesor = idProfesor.Value
                });
            }

            await this.context.SaveChangesAsync();
            return asignatura;
        }

        public async Task<List<VistaUsuariosConRoles>> GetProfesoresYTutoresAsync()
        {
            var rows = await this.context.VistaUsuariosConRoles
                .Where(u => u.Rol == "Profesor" || u.Rol == "Tutor")
                .ToListAsync();

            // Deduplicar por IdUsuario (un usuario puede tener ambos roles)
            return rows.GroupBy(u => u.IdUsuario)
                .Select(g => g.First())
                .OrderBy(u => u.Apellidos)
                .ToList();
        }

        public async Task<Asignatura?> UpdateAsignaturaAsync(int idAsignatura, string nombre)
        {
            var asignatura = await this.context.Asignaturas.FindAsync(idAsignatura);
            if (asignatura == null) return null;
            asignatura.Nombre = nombre;
            await this.context.SaveChangesAsync();
            return asignatura;
        }

        public async Task<bool> DeleteAsignaturaAsync(int idAsignatura)
        {
            var asignatura = await this.context.Asignaturas
                .Include(a => a.Temas).ThenInclude(t => t.Contenidos)
                .FirstOrDefaultAsync(a => a.IdAsignatura == idAsignatura);
            if (asignatura == null) return false;

            using var tx = await this.context.Database.BeginTransactionAsync();
            try
            {
                foreach (var tema in asignatura.Temas.ToList())
                    foreach (var c in tema.Contenidos.ToList())
                        await DeleteContenidoCascadeAsync(c.IdContenido);

                this.context.Temas.RemoveRange(asignatura.Temas);

                var relaciones = this.context.ProfesoresAsignaturas
                    .Where(pa => pa.IdAsignatura == idAsignatura);
                this.context.ProfesoresAsignaturas.RemoveRange(relaciones);

                this.context.Asignaturas.Remove(asignatura);
                await this.context.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                return false;
            }
        }

        public async Task<(bool added, bool conflict)> AddProfesorAsignaturaAsync(int idAsignatura, int idProfesor)
        {
            var exists = await this.context.ProfesoresAsignaturas
                .AnyAsync(pa => pa.IdAsignatura == idAsignatura && pa.IdProfesor == idProfesor);
            if (exists) return (false, true);

            this.context.ProfesoresAsignaturas.Add(new ProfesorAsignatura
            {
                IdAsignatura = idAsignatura,
                IdProfesor = idProfesor
            });
            await this.context.SaveChangesAsync();
            return (true, false);
        }

        public async Task<bool> RemoveProfesorAsignaturaAsync(int idAsignatura, int idProfesor)
        {
            var row = await this.context.ProfesoresAsignaturas
                .FirstOrDefaultAsync(pa => pa.IdAsignatura == idAsignatura && pa.IdProfesor == idProfesor);
            if (row == null) return false;
            this.context.ProfesoresAsignaturas.Remove(row);
            await this.context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Usuario>> GetAlumnosPorCursoAsync(int idCurso)
        {
            return await this.context.Usuarios
                .Where(u => this.context.Inscripciones.Any(i => i.IdCurso == idCurso && i.IdEstudiante == u.IdUsuario))
                .ToListAsync();
        }

        public async Task<List<VistaUsuariosConRoles>> GetProfesoresAsync()
        {
            return await this.context.VistaUsuariosConRoles
                .Where(u => u.Rol == "Profesor")
                .ToListAsync();
        }

        public async Task<AsignaturaDetalleViewModel> GetDetallesAsignatura(int idAsignatura)
        {
            var datos = await this.context.VistaDetallesAsignatura
                .Where(a => a.IdAsignatura == idAsignatura)
                .ToListAsync();

            // Si la vista devuelve filas, la asignatura tiene profesores asignados — camino normal
            if (datos.Any())
            {
                var primerRegistro = datos.First();
                return new AsignaturaDetalleViewModel
                {
                    IdAsignatura = primerRegistro.IdAsignatura,
                    NombreAsignatura = primerRegistro.NombreAsignatura,
                    Profesores = datos
                        .GroupBy(p => p.IdProfesor)
                        .Select(g => new ProfesorViewModel
                        {
                            IdProfesor = g.Key,
                            NombreProfesor = g.First().NombreProfesor,
                            ApellidosProfesor = g.First().ApellidosProfesor,
                            FotoPerfil = g.First().FotoPerfil
                        })
                        .ToList(),
                    Temas = datos
                        .Where(t => t.IdTema.HasValue)
                        .GroupBy(t => new { t.IdTema, t.NombreTema, t.OrdenTema })
                        .Select(tg => new TemaViewModel
                        {
                            IdTema = tg.Key.IdTema,
                            NombreTema = tg.Key.NombreTema,
                            OrdenTema = tg.Key.OrdenTema,
                            Contenidos = tg
                                .Where(c => c.IDContenido.HasValue)
                                .Select(c => new ContenidoViewModel
                                {
                                    IdContenido = c.IDContenido,
                                    TituloContenido = c.TituloContenido,
                                    DescripcionContenido = c.DescripcionContenido,
                                    TipoContenido = c.TipoContenido,
                                    Orden_Contenido = c.OrdenContenido,
                                    Contenido_Completado = c.ContenidoCompletado
                                })
                                .OrderBy(c => c.Orden_Contenido)
                                .ToList()
                        })
                        .OrderBy(t => t.OrdenTema)
                        .ToList()
                };
            }

            // La vista no devuelve filas: asignatura sin profesores asignados.
            // Comprobamos si la asignatura existe para distinguir 404 real de vacía.
            var asignatura = await this.context.Asignaturas
                .Include(a => a.Temas)
                    .ThenInclude(t => t.Contenidos)
                .FirstOrDefaultAsync(a => a.IdAsignatura == idAsignatura);

            if (asignatura == null)
                return null;

            return new AsignaturaDetalleViewModel
            {
                IdAsignatura = asignatura.IdAsignatura,
                NombreAsignatura = asignatura.Nombre,
                Profesores = [],
                Temas = (asignatura.Temas ?? [])
                    .Select(t => new TemaViewModel
                    {
                        IdTema = t.IdTema,
                        NombreTema = t.Nombre,
                        OrdenTema = t.Orden,
                        Contenidos = (t.Contenidos ?? [])
                            .Select(c => new ContenidoViewModel
                            {
                                IdContenido = c.IdContenido,
                                TituloContenido = c.Titulo,
                                DescripcionContenido = c.Descripcion,
                                TipoContenido = c.Tipo,
                                Orden_Contenido = c.Orden,
                                Contenido_Completado = false
                            })
                            .OrderBy(c => c.Orden_Contenido)
                            .ToList()
                    })
                    .OrderBy(t => t.OrdenTema)
                    .ToList()
            };
        }

        public async Task CreateCourseAsync(string nombre, string? descripcion
            , int idProfesor, DateTime fechaInicio, DateTime FechaFin, string Estado, string? imagenPortada)
        {
            Curso curso = new Curso();
            curso.IdCurso = await this.GetMaxIdCourseAsync();
            curso.Nombre = nombre;
            curso.Descripcion = descripcion;
            curso.IdProfesor = idProfesor;
            curso.FechaInicio = fechaInicio;
            curso.FechaFin = FechaFin;
            curso.Estado = Estado;
            curso.ImagenPortada = imagenPortada;
            await this.context.Cursos.AddAsync(curso);
            await this.context.SaveChangesAsync();
        }

        public async Task<bool> UpdateCourseAsync(int idCurso, string nombre, string? descripcion,
                int idProfesor, int? idSuplente, DateTime fechaInicio, DateTime fechaFin, string estado, string? imagenPortada)
        {
            Curso curso = await this.context.Cursos.FindAsync(idCurso);

            if (curso == null)
            {
                return false;
            }

            curso.Nombre = nombre;
            curso.Descripcion = descripcion;
            curso.IdProfesor = idProfesor;
            curso.IdSuplente = idSuplente;
            curso.FechaInicio = fechaInicio;
            curso.FechaFin = fechaFin;
            curso.Estado = estado;

            if (imagenPortada != null)
            {
                curso.ImagenPortada = imagenPortada;
            }

            await this.context.SaveChangesAsync();
            return true;
        }

        #endregion

        #region AREA PERSONAL PROFESOR
        private async Task<int> GetMaxIdCourseAsync()
        {
            if (this.context.Cursos.Count() == 0)
            {
                return 1;
            }
            else
            {
                return await this.context.Cursos.MaxAsync
                    (x => x.IdCurso) + 1;
            }
        }

        private async Task<int> GetMaxIdTemaAsync()
        {
            if (this.context.Temas.Count() == 0)
            {
                return 1;
            }
            else
            {
                return await this.context.Temas.MaxAsync
                    (x => x.IdTema) + 1;
            }
        }

        public async Task CreateTemaAsync(int idAsignatura, string nombre, int Orden)
        {
            Tema tema = new Tema();
            tema.IdTema = await this.GetMaxIdTemaAsync();
            tema.IdAsignatura = idAsignatura;
            tema.Nombre = nombre;
            tema.Orden = Orden;
            await this.context.Temas.AddAsync(tema);
            await this.context.SaveChangesAsync();
        }

        public async Task<bool> DeleteTemaAsync(int idTema)
        {
            var tema = await this.context.Temas
                .Include(t => t.Contenidos)
                .FirstOrDefaultAsync(t => t.IdTema == idTema);
            if (tema == null) return false;

            using var tx = await this.context.Database.BeginTransactionAsync();
            try
            {
                foreach (var c in tema.Contenidos.ToList())
                    await DeleteContenidoCascadeAsync(c.IdContenido);

                this.context.Temas.Remove(tema);
                await this.context.SaveChangesAsync();
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> DeleteContenidoAsync(int idContenido)
        {
            using var tx = await this.context.Database.BeginTransactionAsync();
            try
            {
                var ok = await DeleteContenidoCascadeAsync(idContenido);
                if (!ok) { await tx.RollbackAsync(); return false; }
                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                return false;
            }
        }

        private async Task<bool> DeleteContenidoCascadeAsync(int idContenido)
        {
            var contenido = await this.context.Contenidos.FindAsync(idContenido);
            if (contenido == null) return false;

            var progreso = this.context.ProgresoInscripciones.Where(p => p.IdContenido == idContenido);
            this.context.ProgresoInscripciones.RemoveRange(progreso);

            var calificaciones = this.context.HistorialCalificaciones.Where(h => h.IdContenido == idContenido);
            var calIds = await calificaciones.Select(h => h.IdCalificacion).ToListAsync();
            var comentarios = this.context.ComentariosCalificaciones.Where(c => calIds.Contains(c.IdCalificacion));
            this.context.ComentariosCalificaciones.RemoveRange(comentarios);
            this.context.HistorialCalificaciones.RemoveRange(calificaciones);

            var entregas = this.context.EntregasTareas.Where(e => e.IdContenido == idContenido);
            this.context.EntregasTareas.RemoveRange(entregas);

            var examenes = this.context.Examenes.Where(e => e.IdContenido == idContenido);
            this.context.Examenes.RemoveRange(examenes);

            this.context.Contenidos.Remove(contenido);
            await this.context.SaveChangesAsync();
            return true;
        }

        private async Task<int> GetMaxIdContenidoAsync()
        {
            if (this.context.Contenidos.Count() == 0)
            {
                return 1;
            }
            else
            {
                return await this.context.Contenidos.MaxAsync(x => x.IdContenido) + 1;
            }
        }

        public async Task<Contenido> CreateContenidoAsync(int idTema, string titulo, string tipo, string urlContenido
            , string descripcion, int orden, DateTime? fechaEntrega = null, decimal? puntuacionMaxima = null)
        {
            Contenido contenido = new Contenido();
            contenido.IdContenido = await this.GetMaxIdContenidoAsync();
            contenido.IdTema = idTema;
            contenido.Titulo = titulo;
            contenido.Tipo = tipo;
            contenido.URLContenido = urlContenido;
            contenido.Descripcion = descripcion;
            contenido.Orden = orden;
            contenido.FechaPublicacion = DateTime.Now;

            if (tipo == "Tarea" || tipo == "Quiz" || tipo == "Examen")
            {
                contenido.FechaEntrega = fechaEntrega;
                contenido.PuntuacionMaxima = puntuacionMaxima;
            }

            await this.context.Contenidos.AddAsync(contenido);
            await this.context.SaveChangesAsync();
            return await this.ObtenerContenidoPorIdAsync(contenido.IdContenido);
        }

        public async Task CreateExamenAsync(int idContenido, int duracionMinutos, DateTime fechaPublicacionNotas
            , int numeroIntentos, decimal PenalizacionIncorrecta)
        {
            Examen examen = new Examen
            {
                IdContenido = idContenido,
                DuracionMinutos = duracionMinutos,
                FechaPublicacionNotas = fechaPublicacionNotas,
                IntentosMaximos = numeroIntentos,
                PenalizacionIncorrecta = PenalizacionIncorrecta
            };

            await this.context.Examenes.AddAsync(examen);
            await this.context.SaveChangesAsync();
        }

        public async Task<Curso> GetCursoPorAsignaturaAsync(int idAsignatura)
        {
            var asignatura = await this.context.Asignaturas
                .FirstOrDefaultAsync(a => a.IdAsignatura == idAsignatura);

            if (asignatura == null)
            {
                return null;
            }

            var curso = await this.context.Cursos
                .FirstOrDefaultAsync(c => c.IdCurso == asignatura.IdCurso);

            return curso;
        }

        public async Task<List<VistaCursosDetalles>> GetCursosByProfesorAsync(int idProfesor)
        {
            return await this.context.VistaCursosDetalles
                .Where(c => c.IdProfesor == idProfesor || c.IdSuplente == idProfesor)
                .ToListAsync();
        }

        public async Task<List<VistaAsignaturasProfesor>> GetAsignaturasByProfesorAsync(int idProfesor)
        {
            // Obtenemos primero los IDs de las asignaturas que imparte el profesor
            var asignaturasIds = await this.context.ProfesoresAsignaturas
                .Where(pa => pa.IdProfesor == idProfesor)
                .Select(pa => pa.IdAsignatura)
                .ToListAsync();

            // Agrupamos la información de VistaDetallesAsignatura para obtener datos básicos
            var asignaturas = await this.context.VistaDetallesAsignatura
                .Where(a => asignaturasIds.Contains(a.IdAsignatura))
                .GroupBy(a => new { a.IdAsignatura, a.NombreAsignatura })
                .Select(g => new VistaAsignaturasProfesor
                {
                    IdAsignatura = g.Key.IdAsignatura,
                    NombreAsignatura = g.Key.NombreAsignatura,
                    NumeroTemas = g.Select(a => a.IdTema).Where(id => id != null).Distinct().Count(),
                    NumeroContenidos = g.Select(a => a.IDContenido).Where(id => id != null).Distinct().Count()
                })
                .ToListAsync();

            // Obtener información de los cursos para cada asignatura
            foreach (var asignatura in asignaturas)
            {
                var cursoInfo = await this.context.Asignaturas
                    .Where(a => a.IdAsignatura == asignatura.IdAsignatura)
                    .Join(this.context.VistaCursosDetalles,
                          a => a.IdCurso,
                          c => c.IdCurso,
                          (a, c) => new {
                              IdCurso = c.IdCurso,
                              NombreCurso = c.NombreCurso,
                              Estado = c.Estado,
                              FechaInicio = c.FechaInicio,
                              FechaFin = c.FechaFin
                          })
                    .FirstOrDefaultAsync();

                if (cursoInfo != null)
                {
                    asignatura.IdCurso = cursoInfo.IdCurso;
                    asignatura.NombreCurso = cursoInfo.NombreCurso;
                    asignatura.Estado = cursoInfo.Estado;
                    asignatura.FechaInicio = cursoInfo.FechaInicio;
                    asignatura.FechaFin = cursoInfo.FechaFin;
                }
            }

            return asignaturas;
        }

        #endregion

        #region AREA PERSONAL ESTUDIANTE

        public async Task<List<AsignaturaUsuarioDTO>> GetAsignaturasByUserAsync(int idUsuario)
        {
            // 🔹 Obtener todas las inscripciones activas del usuario
            var inscripciones = await this.context.Inscripciones
                .Where(i => i.IdEstudiante == idUsuario && i.Estado == "Activo")
                .Include(i => i.Estudiante)
                .Include(i => i.Curso)
                .ThenInclude(c => c.Asignaturas)
                .ToListAsync();

            // 🔹 Obtener los profesores en un solo query para optimizar rendimiento
            var profesoresDict = await this.context.ProfesoresAsignaturas
                .Include(pa => pa.Profesor)
                .GroupBy(pa => pa.IdAsignatura)
                .ToDictionaryAsync(g => g.Key, g => $"{g.First().Profesor.Nombre} {g.First().Profesor.Apellidos}");

            // 🔹 Obtener el progreso del usuario en cada asignatura
            var progresoDict = await this.context.ProgresoInscripciones
                .Where(p => p.Completo)
                .GroupBy(p => p.IdInscripcion)
                .ToDictionaryAsync(g => g.Key, g => g.Count());

            var resultado = new List<AsignaturaUsuarioDTO>();

            foreach (var inscripcion in inscripciones)
            {
                foreach (var asignatura in inscripcion.Curso.Asignaturas)
                {
                    var nombreProfesor = profesoresDict.ContainsKey(asignatura.IdAsignatura)
                        ? profesoresDict[asignatura.IdAsignatura]
                        : "Sin profesor asignado";

                    // 🔹 Contar temas y contenidos de la asignatura
                    var temasCount = await this.context.Temas
                        .CountAsync(t => t.IdAsignatura == asignatura.IdAsignatura);

                    var contenidosTotales = await this.context.Contenidos
                        .Where(c => c.Tema.IdAsignatura == asignatura.IdAsignatura)
                        .CountAsync();

                    var contenidosCompletados = progresoDict.ContainsKey(inscripcion.IdInscripcion)
                        ? progresoDict[inscripcion.IdInscripcion]
                        : 0;

                    var progreso = contenidosTotales > 0
                        ? (decimal)contenidosCompletados / contenidosTotales * 100
                        : 0;

                    resultado.Add(new AsignaturaUsuarioDTO
                    {
                        IdUsuario = idUsuario,
                        NombreUsuario = inscripcion.Estudiante.Nombre,
                        IdCurso = inscripcion.IdCurso,
                        NombreCurso = inscripcion.Curso.Nombre,
                        IdAsignatura = asignatura.IdAsignatura,
                        NombreAsignatura = asignatura.Nombre,
                        NombreProfesor = nombreProfesor,
                        FechaInicio = inscripcion.Curso.FechaInicio,
                        FechaFin = inscripcion.Curso.FechaFin,
                        Estado = inscripcion.Curso.Estado,
                        NumeroTemas = temasCount,
                        Progreso = progreso
                    });
                }
            }

            return resultado;
        }

        #endregion

        #region CONTENIDOS

        public async Task<Contenido> ObtenerContenidoPorIdAsync(int id)
        {
            return await this.context.Contenidos
        .Include(c => c.Tema)
        .ThenInclude(t => t.Asignatura)
        .ThenInclude(t => t.Curso)
        .FirstOrDefaultAsync(c => c.IdContenido == id);
        }

        public async Task<bool> UsuarioTieneAccesoAsync(int idContenido, int usuarioId, bool esAdmin)
        {
            if (esAdmin) return true;

            var contenido = await ObtenerContenidoPorIdAsync(idContenido);
            if (contenido == null) return false;

            if (contenido.Tema.Asignatura.Curso.IdProfesor == usuarioId ||
                contenido.Tema.Asignatura.Curso.IdSuplente == usuarioId)
                return true;

            bool esProfesorAsignatura = await this.context.ProfesoresAsignaturas
                .AnyAsync(pa => pa.IdAsignatura == contenido.Tema.IdAsignatura && pa.IdProfesor == usuarioId);

            if (esProfesorAsignatura) return true;

            bool estaInscrito = await this.context.Inscripciones
                .AnyAsync(i => i.IdCurso == contenido.Tema.Asignatura.IdCurso &&
                               i.IdEstudiante == usuarioId &&
                               i.Estado == "Activo");

            return estaInscrito;
        }

        public async Task RegistrarVisualizacionRecursoAsync(int idUsuario, int idContenido)
        {
            var cursoId = await this.context.Contenidos
                .Where(c => c.IdContenido == idContenido)
                .Select(c => c.Tema.Asignatura.IdCurso)
                .FirstOrDefaultAsync();

            if (cursoId == 0) return;

            var inscripcion = await this.context.Inscripciones
                .FirstOrDefaultAsync(i => i.IdCurso == cursoId && i.IdEstudiante == idUsuario);

            if (inscripcion == null) return;

            var progreso = await this.context.ProgresoInscripciones
                .FirstOrDefaultAsync(p => p.IdInscripcion == inscripcion.IdInscripcion && p.IdContenido == idContenido);

            if (progreso == null)
            {
                progreso = new ProgresoInscripcion
                {
                    IdInscripcion = inscripcion.IdInscripcion,
                    IdContenido = idContenido,
                    Completo = true,
                    FechaCompletado = DateTime.Now
                };

                this.context.ProgresoInscripciones.Add(progreso);
                await this.context.SaveChangesAsync();
            }
        }

        public async Task<int?> ObtenerCursoPorContenidoAsync(int idContenido)
        {
            return await this.context.Contenidos
                .Include(c => c.Tema)
                .ThenInclude(t => t.Asignatura)
                .Where(c => c.IdContenido == idContenido)
                .Select(c => c.Tema.Asignatura.IdCurso)
                .FirstOrDefaultAsync();
        }

        public async Task<Inscripcion> ObtenerInscripcionAsync(int cursoId, int usuarioId)
        {
            return await this.context.Inscripciones
                .FirstOrDefaultAsync(i => i.IdCurso == cursoId && i.IdEstudiante == usuarioId);
        }

        public async Task<ProgresoInscripcion> ObtenerProgresoAsync(int inscripcionId, int idContenido)
        {
            return await this.context.ProgresoInscripciones
                .FirstOrDefaultAsync(p => p.IdInscripcion == inscripcionId && p.IdContenido == idContenido);
        }

        public async Task RegistrarProgresoAsync(ProgresoInscripcion progreso)
        {
            this.context.ProgresoInscripciones.Add(progreso);
            await this.context.SaveChangesAsync();
        }

        public async Task GuardarCambiosAsync()
        {
            await this.context.SaveChangesAsync();
        }

        #endregion

        #region TAREAS

        public async Task<TareaViewModel> ObtenerTareaDetalleAsync(int contenidoId, int usuarioId)
        {
            // Obtenemos el contenido con sus relaciones
            var contenido = await this.context.Contenidos
                .Include(c => c.Tema)
                    .ThenInclude(t => t.Asignatura)
                .FirstOrDefaultAsync(c => c.IdContenido == contenidoId);

            if (contenido == null)
                return null;

            // Obtenemos la entrega del usuario si existe
            var entrega = await this.context.EntregasTareas
                .Where(e => e.IdContenido == contenidoId && e.IdEstudiante == usuarioId)
                .OrderByDescending(e => e.FechaEntrega)
                .FirstOrDefaultAsync();

            // Obtenemos la calificación si existe
            var calificacion = entrega != null ?
                await this.context.HistorialCalificaciones
                    .Where(c => c.IdContenido == contenidoId && c.IdEstudiante == usuarioId)
                    .OrderByDescending(c => c.FechaCalificacion)
                    .FirstOrDefaultAsync() : null;

            // Obtenemos comentarios si existen
            string comentarios = null;
            if (calificacion != null)
            {
                var comentario = await this.context.ComentariosCalificaciones
                    .Where(c => c.IdCalificacion == calificacion.IdCalificacion)
                    .OrderByDescending(c => c.FechaComentario)
                    .FirstOrDefaultAsync();

                if (comentario != null)
                    comentarios = comentario.Comentario;
            }

            // Construimos el ViewModel
            var tareaViewModel = new TareaViewModel
            {
                IdContenido = contenido.IdContenido,
                IdTema = contenido.IdTema,
                NombreTema = contenido.Tema.Nombre,
                IdAsignatura = contenido.Tema.IdAsignatura,
                NombreAsignatura = contenido.Tema.Asignatura.Nombre,
                Titulo = contenido.Titulo,
                Descripcion = contenido.Descripcion,
                UrlContenido = contenido.URLContenido,
                FechaEntrega = contenido.FechaEntrega ?? DateTime.Now.AddDays(7), // Default por si no tiene fecha
                PuntuacionMaxima = contenido.PuntuacionMaxima ?? 10, // Default por si no tiene puntuación
                PuntuacionAprobado = (contenido.PuntuacionMaxima ?? 10) * 0.6m, // 60% para aprobar
                PermiteEntregaTardia = true // Esto podría configurarse a nivel de curso o contenido
            };

            // Si hay entrega, agregamos datos al ViewModel
            if (entrega != null)
            {
                tareaViewModel.Entrega = new EntregaTareaViewModel
                {
                    IdEntrega = entrega.IdEntrega,
                    URLEntrega = entrega.URLEntrega,
                    FechaEntrega = entrega.FechaEntrega,
                    Estado = entrega.Estado,
                    Calificacion = calificacion?.Calificacion,
                    Comentarios = comentarios
                };
            }

            return tareaViewModel;
        }

        public async Task<List<EntregaTareaViewModel>> ObtenerEntregasTareaAsync(int contenidoId)
        {
            var entregas = await this.context.EntregasTareas
                .Where(e => e.IdContenido == contenidoId)
                .OrderByDescending(e => e.FechaEntrega)
                .Select(e => new EntregaTareaViewModel
                {
                    IdEntrega = e.IdEntrega,
                    IdEstudiante = e.IdEstudiante,
                    NombreEstudiante = this.context.Usuarios
                        .Where(u => u.IdUsuario == e.IdEstudiante)
                        .Select(u => u.Nombre + " " + u.Apellidos)
                        .FirstOrDefault(),
                    URLEntrega = e.URLEntrega,
                    FechaEntrega = e.FechaEntrega,
                    Estado = e.Estado,
                    Calificacion = this.context.HistorialCalificaciones
                        .Where(c => c.IdContenido == contenidoId && c.IdEstudiante == e.IdEstudiante)
                        .OrderByDescending(c => c.FechaCalificacion)
                        .Select(c => c.Calificacion)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return entregas;
        }

        public async Task<int> GuardarCalificacionAsync(int idEntrega, int idEstudiante, int idContenido,
                decimal calificacion, string comentarios, int idProfesor)
        {
            // Obtener nuevo ID para la calificación
            int idCalificacion = await GetMaxIdHistorialCalificacionesAsync();

            // Crear y guardar la calificación
            var nuevaCalificacion = new HistorialCalificacion
            {
                IdCalificacion = idCalificacion,
                IdContenido = idContenido,
                IdEstudiante = idEstudiante,
                Calificacion = calificacion,
                FechaCalificacion = DateTime.Now,
                IdProfesorCalificador = idProfesor
            };

            this.context.HistorialCalificaciones.Add(nuevaCalificacion);
            await this.context.SaveChangesAsync();

            // Si hay comentarios, guardarlos
            if (!string.IsNullOrEmpty(comentarios))
            {
                int idComentario = await GetMaxIdComentariosCalificacionesAsync();

                var nuevoComentario = new ComentarioCalificacion
                {
                    IdComentario = idComentario,
                    IdCalificacion = idCalificacion,
                    IdAutor = idProfesor,
                    Comentario = comentarios,
                    FechaComentario = DateTime.Now
                };

                this.context.ComentariosCalificaciones.Add(nuevoComentario);
                await this.context.SaveChangesAsync();
            }

            // Actualizar el estado de la entrega
            var entrega = await this.context.EntregasTareas.FindAsync(idEntrega);
            if (entrega != null)
            {
                entrega.Estado = "Revisado";
                await this.context.SaveChangesAsync();
            }

            return idCalificacion;
        }

        private async Task<int> GetMaxIdHistorialCalificacionesAsync()
        {
            if (!await this.context.HistorialCalificaciones.AnyAsync())
            {
                return 1;
            }
            else
            {
                return await this.context.HistorialCalificaciones.MaxAsync(x => x.IdCalificacion) + 1;
            }
        }

        private async Task<int> GetMaxIdComentariosCalificacionesAsync()
        {
            if (!await this.context.ComentariosCalificaciones.AnyAsync())
            {
                return 1;
            }
            else
            {
                return await this.context.ComentariosCalificaciones.MaxAsync(x => x.IdComentario) + 1;
            }
        }

        public async Task<bool> ActualizarTareaAsync(int contenidoId, TareaEditViewModel model, string urlContenido = null)
        {
            var contenido = await this.context.Contenidos
                .FirstOrDefaultAsync(c => c.IdContenido == contenidoId);

            if (contenido == null)
                return false;

            contenido.Titulo = model.Titulo;
            contenido.Descripcion = model.Descripcion;
            contenido.FechaEntrega = model.FechaEntrega;
            contenido.PuntuacionMaxima = model.PuntuacionMaxima;

            // Solo actualizar la URL si se ha proporcionado una nueva
            if (!string.IsNullOrEmpty(urlContenido))
            {
                contenido.URLContenido = urlContenido;
            }

            // Aquí podrías actualizar la propiedad PermiteEntregaTardia si la tienes en tu modelo de datos
            // contenido.PermiteEntregaTardia = model.PermiteEntregaTardia;

            await this.context.SaveChangesAsync();
            return true;
        }

        private async Task<int> GetMaxIdEntregaTareasAsync()
        {
            if (this.context.EntregasTareas.Count() == 0)
            {
                return 1;
            }
            else
            {
                return await this.context.EntregasTareas.MaxAsync
                    (x => x.IdEntrega) + 1;
            }
        }

        public async Task<bool> GuardarEntregaAsync(int contenidoId, int usuarioId, string comentario, IFormFile archivo)
        {
            try
            {
                if (archivo == null || archivo.Length == 0)
                    return false;

                // Verificar tamaño máximo (10MB)
                if (archivo.Length > 10 * 1024 * 1024)
                    return false;

                // Generar nombre único para el archivo
                var ext1 = Path.GetExtension(archivo.FileName).ToLower();
                string fileName = Guid.NewGuid().ToString("N") + ext1;
                string filePath = this.helperPath.MapPath(fileName, Folders.contents);

                // Guardar archivo
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                // Guardar información en la base de datos
                var entrega = new EntregaTarea
                {
                    IdEntrega = await this.GetMaxIdEntregaTareasAsync(),
                    IdContenido = contenidoId,
                    IdEstudiante = usuarioId,
                    URLEntrega = fileName,
                    FechaEntrega = DateTime.Now,
                    Estado = "Pendiente"
                };

                this.context.EntregasTareas.Add(entrega);
                await this.context.SaveChangesAsync();

                // Actualizar progreso del estudiante
                await ActualizarProgresoEstudianteAsync(contenidoId, usuarioId);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ActualizarEntregaAsync(int contenidoId, int usuarioId, string comentario, IFormFile archivo)
        {
            try
            {
                // Verificar si existe una entrega previa
                var entregaPrevia = await this.context.EntregasTareas
                    .Where(e => e.IdContenido == contenidoId && e.IdEstudiante == usuarioId)
                    .OrderByDescending(e => e.FechaEntrega)
                    .FirstOrDefaultAsync();

                if (entregaPrevia == null)
                    return await GuardarEntregaAsync(contenidoId, usuarioId, comentario, archivo);

                if (archivo == null || archivo.Length == 0)
                    return false;

                // Verificar tamaño máximo (10MB)
                if (archivo.Length > 10 * 1024 * 1024)
                    return false;

                // Generar nombre único para el archivo
                var ext2 = Path.GetExtension(archivo.FileName).ToLower();
                string fileName = Guid.NewGuid().ToString("N") + ext2;
                string filePath = this.helperPath.MapPath(fileName, Folders.contents);

                // Guardar archivo
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await archivo.CopyToAsync(stream);
                }

                // Actualizar información en la base de datos
                entregaPrevia.URLEntrega = fileName;
                entregaPrevia.FechaEntrega = DateTime.Now;
                entregaPrevia.Estado = "Pendiente";

                await this.context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task ActualizarProgresoEstudianteAsync(int contenidoId, int usuarioId)
        {
            // Buscar inscripción del estudiante
            var contenido = await this.context.Contenidos
                .Include(c => c.Tema)
                    .ThenInclude(t => t.Asignatura)
                .FirstOrDefaultAsync(c => c.IdContenido == contenidoId);

            if (contenido == null)
                return;

            // Buscar inscripción en el curso correspondiente
            var inscripcion = await this.context.Inscripciones
                .FirstOrDefaultAsync(i => i.IdCurso == contenido.Tema.Asignatura.IdCurso &&
                                         i.IdEstudiante == usuarioId);

            if (inscripcion == null)
                return;

            // Verificar si ya existe un registro de progreso
            var progreso = await this.context.ProgresoInscripciones
                .FirstOrDefaultAsync(p => p.IdInscripcion == inscripcion.IdInscripcion &&
                                        p.IdContenido == contenidoId);

            if (progreso == null)
            {
                // Crear nuevo registro de progreso
                progreso = new ProgresoInscripcion
                {
                    IdInscripcion = inscripcion.IdInscripcion,
                    IdContenido = contenidoId,
                    Completo = true,
                    FechaCompletado = DateTime.Now
                };
                this.context.ProgresoInscripciones.Add(progreso);
            }
            else
            {
                // Actualizar registro existente
                progreso.Completo = true;
                progreso.FechaCompletado = DateTime.Now;
            }

            // Actualizar porcentaje de curso completado
            await ActualizarPorcentajeCursoAsync(inscripcion.IdInscripcion);

            await this.context.SaveChangesAsync();
        }

        private async Task ActualizarPorcentajeCursoAsync(int inscripcionId)
        {
            // Obtener todos los contenidos del curso
            var inscripcion = await this.context.Inscripciones
                .FirstOrDefaultAsync(i => i.IdInscripcion == inscripcionId);

            if (inscripcion == null)
                return;

            // Obtener todos los contenidos del curso
            var contenidos = await this.context.Contenidos
                .Include(c => c.Tema)
                    .ThenInclude(t => t.Asignatura)
                .Where(c => this.context.Asignaturas
                    .Where(a => a.IdCurso == inscripcion.IdCurso)
                    .Select(a => a.IdAsignatura)
                    .Contains(c.Tema.IdAsignatura))
                .ToListAsync();

            // Obtener todos los contenidos completados
            var completados = await this.context.ProgresoInscripciones
                .Where(p => p.IdInscripcion == inscripcionId && p.Completo)
                .Select(p => p.IdContenido)
                .ToListAsync();

            // Calcular porcentaje
            decimal totalContenidos = contenidos.Count;
            decimal completadosCount = completados.Count;
            decimal porcentaje = totalContenidos > 0 ? (completadosCount / totalContenidos) * 100 : 0;

            // Actualizar porcentaje en la inscripción
            inscripcion.PorcentajeCompletado = porcentaje;
            await this.context.SaveChangesAsync();
        }

        #endregion

        #region CALENDARIO

        public async Task<List<object>> GetCalendarEventsAsync(int idUsuario, string role)
        {
            var events = new List<object>();

            if (role == "Estudiante")
            {
                var inscripciones = await this.context.Inscripciones
                    .Where(i => i.IdEstudiante == idUsuario && i.Estado == "Activo")
                    .Include(i => i.Curso)
                    .ToListAsync();

                foreach (var ins in inscripciones)
                {
                    events.Add(new
                    {
                        fecha = ins.Curso.FechaInicio.ToString("yyyy-MM-dd"),
                        tipo = "inicio_curso",
                        titulo = ins.Curso.Nombre,
                        idCurso = ins.Curso.IdCurso
                    });
                    events.Add(new
                    {
                        fecha = ins.Curso.FechaFin.ToString("yyyy-MM-dd"),
                        tipo = "fin_curso",
                        titulo = ins.Curso.Nombre,
                        idCurso = ins.Curso.IdCurso
                    });

                    var asignaturaIds = await this.context.Asignaturas
                        .Where(a => a.IdCurso == ins.IdCurso)
                        .Select(a => a.IdAsignatura)
                        .ToListAsync();

                    var tareas = await this.context.Contenidos
                        .Include(c => c.Tema)
                        .Where(c => asignaturaIds.Contains(c.Tema.IdAsignatura)
                                    && c.FechaEntrega != null
                                    && (c.Tipo == "Tarea" || c.Tipo == "Quiz" || c.Tipo == "Examen"))
                        .ToListAsync();

                    foreach (var t in tareas)
                    {
                        events.Add(new
                        {
                            fecha = t.FechaEntrega!.Value.ToString("yyyy-MM-dd"),
                            tipo = "entrega",
                            tipoContenido = t.Tipo,
                            titulo = t.Titulo,
                            idCurso = ins.Curso.IdCurso
                        });
                    }
                }
            }
            else
            {
                var asignaturaIds = await this.context.ProfesoresAsignaturas
                    .Where(pa => pa.IdProfesor == idUsuario)
                    .Select(pa => pa.IdAsignatura)
                    .ToListAsync();

                var cursoIds = await this.context.Asignaturas
                    .Where(a => asignaturaIds.Contains(a.IdAsignatura))
                    .Select(a => a.IdCurso)
                    .Distinct()
                    .ToListAsync();

                var cursos = await this.context.Cursos
                    .Where(c => cursoIds.Contains(c.IdCurso) && c.Estado != "Eliminado")
                    .ToListAsync();

                foreach (var c in cursos)
                {
                    events.Add(new { fecha = c.FechaInicio.ToString("yyyy-MM-dd"), tipo = "inicio_curso", titulo = c.Nombre, idCurso = c.IdCurso });
                    events.Add(new { fecha = c.FechaFin.ToString("yyyy-MM-dd"), tipo = "fin_curso", titulo = c.Nombre, idCurso = c.IdCurso });
                }

                var tareas = await this.context.Contenidos
                    .Include(ct => ct.Tema)
                    .Where(ct => asignaturaIds.Contains(ct.Tema.IdAsignatura)
                                && ct.FechaEntrega != null
                                && (ct.Tipo == "Tarea" || ct.Tipo == "Quiz" || ct.Tipo == "Examen"))
                    .ToListAsync();

                foreach (var t in tareas)
                {
                    var idCurso = await this.context.Asignaturas
                        .Where(a => a.IdAsignatura == t.Tema.IdAsignatura)
                        .Select(a => a.IdCurso)
                        .FirstOrDefaultAsync();
                    events.Add(new { fecha = t.FechaEntrega!.Value.ToString("yyyy-MM-dd"), tipo = "entrega", tipoContenido = t.Tipo, titulo = t.Titulo, idCurso });
                }
            }

            return events;
        }

        #endregion

        #region EXAMENES

        //public async Task<Contenido?> ObtenerExamenPorIdAsync(int id)
        //{
        //    return await this.context.Contenidos
        //        .Include(c => c.Tema)
        //        .ThenInclude(t => t.Asignatura)
        //        .Include(c => c.Examen)
        //        .FirstOrDefaultAsync(c => c.IdContenido == id && c.Tipo == "Examen");
        //}

        //public async Task<List<IntentoViewModel>> ObtenerIntentosUsuarioAsync(int idExamen, int idUsuario)
        //{
        //    return await this.context.ExamenesUsuarios
        //        .Where(e => e.IdContenido == idExamen && e.IdEstudiante == idUsuario)
        //        .OrderByDescending(e => e.Fecha_Inicio)
        //        .Select(e => new IntentoViewModel
        //        {
        //            ID_Intento = e.ID_Intento,
        //            Fecha_Inicio = e.Fecha_Inicio,
        //            Fecha_Fin = e.Fecha_Fin,
        //            Completado = e.Fecha_Fin.HasValue,
        //            Duracion = e.Fecha_Fin.HasValue ? (e.Fecha_Fin.Value - e.Fecha_Inicio).TotalMinutes : 0
        //        })
        //        .ToListAsync();
        //}

        //public async Task<decimal?> ObtenerCalificacionUsuarioAsync(int idExamen, int idUsuario)
        //{
        //    var calificacion = await this.context.ExamenesUsuarios
        //        .Where(e => e.IdContenido == idExamen && e.IdEstudiante == idUsuario)
        //        .OrderByDescending(e => e.FechaCalificacion)
        //        .Select(e => e.Calificacion)
        //        .FirstOrDefaultAsync();

        //    return calificacion;
        //}

        #endregion
    }
}
