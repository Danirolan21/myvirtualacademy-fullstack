import client from './client'

export const createTopic = (data: { nombre: string; orden: number; idAsignatura: number }) =>
  client.post('/api/topics', data)
