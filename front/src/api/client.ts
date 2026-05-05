import axios from 'axios'

const API_BASE = import.meta.env.VITE_API_URL ?? ''

const client = axios.create({ baseURL: API_BASE, withCredentials: true })

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
        const res = await axios.post(
          `${API_BASE}/api/auth/refresh`,
          {},
          { withCredentials: true }
        )
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
