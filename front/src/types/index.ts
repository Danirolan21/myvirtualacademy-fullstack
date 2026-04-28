export interface AuthUser {
  id: number
  nombre: string
  apellidos: string
  role: string
  fotoPerfil: string
  isAdmin: boolean
}

export interface Usuario {
  idUsuario: number
  nombre: string
  apellidos: string
  email: string
  telefono?: string
  fechaRegistro: string
  ultimoAcceso?: string
  fotoPerfilUrl?: string
}

export interface Rol {
  idRol: number
  nombre: string
}

export interface VistaCursosDetalles {
  idCurso: number
  nombreCurso: string
  descripcion?: string
  idProfesor: number
  idSuplente?: number
  fechaInicio: string
  fechaFin: string
  estado: string
  imagenPortada?: string
  nombreProfesor: string
  nombreSuplente?: string
  numeroAsignaturas: number
  numeroAlumnos: number
}

export interface Asignatura {
  idAsignatura: number
  nombre: string
  idCurso: number
}

export interface ProfesorVM {
  idProfesor: number
  nombreProfesor: string
  apellidosProfesor: string
  fotoPerfil?: string
}

export interface ContenidoVM {
  idContenido?: number
  tituloContenido: string
  descripcionContenido?: string
  tipoContenido: string
  orden_Contenido?: number
  contenido_Completado?: boolean
}

export interface TemaVM {
  idTema?: number
  idAsignatura: number
  nombreTema: string
  ordenTema?: number
  contenidos: ContenidoVM[]
}

export interface AsignaturaDetalle {
  idAsignatura: number
  nombreAsignatura: string
  profesores: ProfesorVM[]
  temas: TemaVM[]
}

export interface AsignaturaUsuarioDTO {
  idUsuario: number
  nombreUsuario: string
  idCurso: number
  nombreCurso: string
  idAsignatura: number
  nombreAsignatura: string
  nombreProfesor: string
  fechaInicio: string
  fechaFin: string
  estado: string
  numeroTemas: number
  progreso: number
}

export interface VistaAsignaturasProfesor {
  idAsignatura: number
  nombreAsignatura: string
  idCurso: number
  nombreCurso: string
  estado: string
  fechaInicio: string
  fechaFin: string
  numeroTemas: number
  numeroContenidos: number
}

export interface EntregaTareaVM {
  idEntrega: number
  idEstudiante?: number
  nombreEstudiante?: string
  urlEntrega: string
  fechaEntrega: string
  estado: string
  calificacion?: number
  comentarios?: string
}

export interface TareaViewModel {
  idContenido: number
  idTema: number
  nombreTema: string
  idAsignatura: number
  nombreAsignatura: string
  titulo: string
  descripcion?: string
  urlContenido?: string
  fechaEntrega: string
  puntuacionMaxima: number
  puntuacionAprobado: number
  permiteEntregaTardia: boolean
  entrega?: EntregaTareaVM
  entregas?: EntregaTareaVM[]
}

export interface RecursoViewModel {
  idContenido: number
  titulo: string
  descripcion?: string
  urlContenido: string
  fechaPublicacion: string
  fechaEntrega?: string
  puntuacionMaxima?: number
  tipo: string
  idTema: number
  nombreTema: string
  idAsignatura: number
  nombreAsignatura: string
}
