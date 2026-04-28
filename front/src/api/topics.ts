import client from './client'

export const createTopic = (data: { nombre: string; orden: number; idAsignatura: number }) =>
  client.post('/api/topics', data)

export const deleteTopic = (id: number) =>
  client.delete(`/api/topics/${id}`)
