import axios from 'axios'

// No baseURL — todas las llamadas van a /api/* y el proxy de Vite (dev)
// o nginx (prod) las enruta al backend
const client = axios.create({ withCredentials: true })

// Adjunta el access token en memoria si existe
client.interceptors.request.use(config => {
  const token = sessionStorage.getItem('__mva_at')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

let isRefreshing = false
let queue: Array<() => void> = []

client.interceptors.response.use(
  res => res,
  async err => {
    const original = err.config
    if (err.response?.status === 401 && !original._retry) {
      original._retry = true

      if (isRefreshing) {
        return new Promise(resolve => queue.push(() => resolve(client(original))))
      }

      isRefreshing = true
      try {
        const res = await axios.post('/api/auth/refresh', {}, { withCredentials: true })
        sessionStorage.setItem('__mva_at', res.data.accessToken)
        queue.forEach(cb => cb())
        queue = []
        return client(original)
      } catch {
        queue = []
        sessionStorage.removeItem('__mva_at')
        window.location.href = '/login'
      } finally {
        isRefreshing = false
      }
    }
    return Promise.reject(err)
  }
)

export default client
