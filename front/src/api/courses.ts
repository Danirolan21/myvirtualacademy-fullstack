import client from './client'
import type { VistaCursosDetalles } from '../types'

export const getCourses = () =>
  client.get<VistaCursosDetalles[]>('/api/courses')

export const getCoursesByProfessor = (profesorId: number) =>
  client.get<VistaCursosDetalles[]>(`/api/courses/by-professor/${profesorId}`)

export const getCourse = (id: number) =>
  client.get<any>(`/api/courses/${id}`)

export const createCourse = (data: FormData) =>
  client.post('/api/courses', data)

export const updateCourse = (id: number, data: FormData) =>
  client.put(`/api/courses/${id}`, data)

export const deleteCourse = (id: number) =>
  client.delete(`/api/courses/${id}`)

export const getEnrollments = (courseId: number) =>
  client.get<any[]>(`/api/courses/${courseId}/enrollments`)

export const enrollStudent = (courseId: number, idEstudiante: number) =>
  client.post(`/api/courses/${courseId}/enrollments`, { idEstudiante })

export const unenrollStudent = (courseId: number, idEstudiante: number) =>
  client.delete(`/api/courses/${courseId}/enrollments/${idEstudiante}`)

export const getAvailableStudents = (courseId: number) =>
  client.get<any[]>(`/api/courses/${courseId}/enrollments/available`)
