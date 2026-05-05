import client from './client'
import type { AsignaturaDetalle, AsignaturaUsuarioDTO, VistaAsignaturasProfesor } from '../types'

export const getSubject = (id: number) =>
  client.get<AsignaturaDetalle>(`/api/subjects/${id}`)

export const getSubjectsByProfessor = (profesorId: number) =>
  client.get<VistaAsignaturasProfesor[]>(`/api/subjects/by-professor/${profesorId}`)

export const getSubjectsByStudent = (estudianteId: number) =>
  client.get<AsignaturaUsuarioDTO[]>(`/api/subjects/by-student/${estudianteId}`)

export const createSubject = (idCurso: number, nombreAsignatura: string, idProfesor?: number) =>
  client.post('/api/subjects', { idCurso, nombreAsignatura, idProfesor: idProfesor ?? null })

export const updateSubject = (id: number, nombreAsignatura: string) =>
  client.put(`/api/subjects/${id}`, { nombreAsignatura })

export const deleteSubject = (id: number) =>
  client.delete(`/api/subjects/${id}`)

export const addProfesorToSubject = (id: number, idProfesor: number) =>
  client.post(`/api/subjects/${id}/profesores`, { idProfesor })

export const removeProfesorFromSubject = (id: number, idProfesor: number) =>
  client.delete(`/api/subjects/${id}/profesores/${idProfesor}`)
