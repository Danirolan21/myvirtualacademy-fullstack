import client from './client'

export interface AdminStats {
  totalCursos: number
  cursosActivos: number
  totalUsuarios: number
  totalEstudiantes: number
  totalProfesores: number
  totalInscripciones: number
  entregasPendientes: number
  actividadReciente: {
    idEntrega: number
    nombreEstudiante: string
    tituloContenido: string
    fechaEntrega: string
    estado: string
  }[]
}

export const getAdminStats = () => client.get<AdminStats>('/api/admin/stats')
