import client from './client'
import type { AsignaturaDetalle, AsignaturaUsuarioDTO, VistaAsignaturasProfesor } from '../types'

export const getSubject = (id: number) =>
  client.get<AsignaturaDetalle>(`/api/subjects/${id}`)

export const getSubjectsByProfessor = (profesorId: number) =>
  client.get<VistaAsignaturasProfesor[]>(`/api/subjects/by-professor/${profesorId}`)

export const getSubjectsByStudent = (estudianteId: number) =>
  client.get<AsignaturaUsuarioDTO[]>(`/api/subjects/by-student/${estudianteId}`)

export const createSubject = (idCurso: number, nombreAsignatura: string) =>
  client.post('/api/subjects', { idCurso, nombreAsignatura })
