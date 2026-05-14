const API_BASE = import.meta.env.VITE_API_URL ?? ''

export function userAvatar(fotoPerfil: string | null | undefined): string {
  if (!fotoPerfil) return ''
  return `${API_BASE}/assets/images/users/${fotoPerfil}`
}

export function courseCover(imagenPortada: string | null | undefined): string {
  if (!imagenPortada) return ''
  return `${API_BASE}/assets/images/courses/${imagenPortada}`
}

export function contentFileUrl(filename: string | null | undefined): string {
  if (!filename) return ''
  if (filename.startsWith('http')) return filename
  return `${API_BASE}/uploads/contents/${filename}`
}
