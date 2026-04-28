<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '../../stores/auth'
import { getUser } from '../../api/users'
import { getCoursesByProfessor } from '../../api/courses'
import { getSubjectsByStudent } from '../../api/subjects'
import { formatDate } from '../../utils/format'
import type { Usuario, VistaCursosDetalles, AsignaturaUsuarioDTO } from '../../types'

const auth = useAuthStore()
const usuario = ref<Usuario | null>(null)
const loading = ref(true)

interface CourseItem {
  idCurso: number
  nombreCurso: string
  estado: string
  imagenPortada?: string
  linkTo?: string
}

const cursos = ref<CourseItem[]>([])

const statusClass: Record<string, string> = {
  Activo: 'badge-activo', Borrador: 'badge-borrador',
  Finalizado: 'badge-finalizado', Archivado: 'badge-archivado', Suspendido: 'badge-suspendido',
}

onMounted(async () => {
  try {
    const [userRes] = await Promise.all([getUser(auth.user!.id)])
    usuario.value = userRes.data

    const role = auth.role
    const userId = auth.user!.id

    if (auth.isAdmin || role === 'Administrador') {
      // Admin: cursos donde es tutor/suplente
      try {
        const res = await getCoursesByProfessor(userId)
        cursos.value = res.data.map((c: VistaCursosDetalles) => ({
          idCurso: c.idCurso,
          nombreCurso: c.nombreCurso,
          estado: c.estado,
          imagenPortada: c.imagenPortada,
          linkTo: `/admin/cursos/${c.idCurso}`,
        }))
      } catch { /* admin puede no tener cursos */ }
    } else if (role === 'Profesor' || role === 'Tutor') {
      const res = await getCoursesByProfessor(userId)
      cursos.value = res.data.map((c: VistaCursosDetalles) => ({
        idCurso: c.idCurso,
        nombreCurso: c.nombreCurso,
        estado: c.estado,
        imagenPortada: c.imagenPortada,
        linkTo: `/profesor`,
      }))
    } else {
      // Estudiante: derivar cursos únicos de asignaturas
      const res = await getSubjectsByStudent(userId)
      const seen = new Set<number>()
      cursos.value = res.data
        .filter((a: AsignaturaUsuarioDTO) => {
          if (seen.has(a.idCurso)) return false
          seen.add(a.idCurso)
          return true
        })
        .map((a: AsignaturaUsuarioDTO) => ({
          idCurso: a.idCurso,
          nombreCurso: a.nombreCurso,
          estado: a.estado,
          linkTo: `/estudiante`,
        }))
    }
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div>
    <div v-if="loading" class="loading-center"><div class="spinner"></div></div>

    <template v-else-if="usuario">
      <!-- Dark profile header -->
      <div class="profile-hero">
        <div class="profile-hero-bg"></div>
        <div class="profile-hero-content container">
          <div class="avatar-wrap">
            <img
              :src="`/assets/images/users/${auth.user?.fotoPerfil}`"
              class="avatar-img"
              alt="Avatar"
            />
            <RouterLink to="/perfil/editar" class="avatar-edit-btn">
              <i class="fas fa-pencil-alt"></i>
            </RouterLink>
          </div>
          <div class="profile-hero-info">
            <h1 class="profile-name">{{ usuario.nombre }} {{ usuario.apellidos }}</h1>
            <span class="profile-role-badge">{{ auth.role }}</span>
          </div>
          <RouterLink to="/perfil/editar" class="btn-edit-profile">
            <i class="fas fa-user-edit"></i> Editar perfil
          </RouterLink>
        </div>
      </div>

      <!-- Two-column content -->
      <div class="container profile-body">

        <!-- Left: Mis Cursos -->
        <section class="profile-section">
          <h2 class="section-heading">
            Mis <span class="heading-accent">Cursos</span>
          </h2>

          <div v-if="cursos.length" class="course-list">
            <RouterLink
              v-for="c in cursos"
              :key="c.idCurso"
              :to="c.linkTo ?? '/'"
              class="course-mini-card"
            >
              <div class="course-mini-cover">
                <img
                  v-if="c.imagenPortada"
                  :src="`/assets/images/courses/${c.imagenPortada}`"
                  :alt="c.nombreCurso"
                />
                <div v-else class="course-mini-cover-fallback">
                  <i class="fas fa-graduation-cap"></i>
                </div>
              </div>
              <div class="course-mini-info">
                <div class="course-mini-name">{{ c.nombreCurso }}</div>
                <span class="badge" :class="statusClass[c.estado] ?? 'badge-secondary'">{{ c.estado }}</span>
              </div>
              <i class="fas fa-chevron-right course-mini-arrow"></i>
            </RouterLink>
          </div>

          <div v-else class="empty-courses">
            <i class="fas fa-book-open empty-icon"></i>
            <p>No tienes cursos asignados actualmente.</p>
          </div>
        </section>

        <!-- Right: Más datos -->
        <section class="profile-section">
          <h2 class="section-heading">
            Más <span class="heading-accent">datos</span>
          </h2>
          <dl class="data-list">
            <div class="data-row">
              <dt class="data-label"><i class="fas fa-envelope"></i> Correo</dt>
              <dd class="data-value">{{ usuario.email }}</dd>
            </div>
            <div class="data-row">
              <dt class="data-label"><i class="fas fa-phone"></i> Teléfono</dt>
              <dd class="data-value">{{ usuario.telefono || 'No especificado' }}</dd>
            </div>
            <div class="data-row">
              <dt class="data-label"><i class="fas fa-calendar-plus"></i> Registro</dt>
              <dd class="data-value">{{ formatDate(usuario.fechaRegistro) }}</dd>
            </div>
            <div class="data-row" v-if="usuario.ultimoAcceso">
              <dt class="data-label"><i class="fas fa-clock"></i> Último acceso</dt>
              <dd class="data-value">{{ formatDate(usuario.ultimoAcceso) }}</dd>
            </div>
          </dl>
        </section>

      </div>
    </template>
  </div>
</template>

<style scoped>
.loading-center { display: flex; justify-content: center; padding: var(--sp-12) 0; }

/* Hero */
.profile-hero {
  position: relative;
  background: #1a1a2e;
  overflow: hidden;
  padding: var(--sp-10) 0 var(--sp-8);
}
.profile-hero-bg {
  position: absolute;
  inset: 0;
  background: url('/assets/images/3d-background-neon-ultraviolet-purple-3840x1080-2562.jpg') center / cover no-repeat;
  opacity: 0.25;
}
.profile-hero-content {
  position: relative;
  display: flex;
  align-items: center;
  gap: var(--sp-6);
  flex-wrap: wrap;
}

.avatar-wrap { position: relative; flex-shrink: 0; }
.avatar-img {
  width: 120px; height: 120px;
  border-radius: 50%;
  object-fit: cover;
  border: 4px solid rgba(255,255,255,0.3);
}
.avatar-edit-btn {
  position: absolute;
  bottom: 4px; right: 4px;
  width: 28px; height: 28px;
  background: var(--color-primary);
  color: white;
  border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-size: 11px;
  text-decoration: none;
  transition: background 0.15s;
}
.avatar-edit-btn:hover { background: #0b5ed7; }

.profile-hero-info { flex: 1; }
.profile-name {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  color: white;
  margin: 0 0 var(--sp-2);
}
.profile-role-badge {
  display: inline-block;
  background: rgba(255,255,255,0.15);
  color: rgba(255,255,255,0.9);
  border: 1px solid rgba(255,255,255,0.25);
  border-radius: 999px;
  padding: var(--sp-1) var(--sp-3);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
}

.btn-edit-profile {
  display: inline-flex;
  align-items: center;
  gap: var(--sp-2);
  padding: var(--sp-2) var(--sp-4);
  background: rgba(255,255,255,0.1);
  border: 1px solid rgba(255,255,255,0.3);
  border-radius: var(--radius-md);
  color: white;
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  text-decoration: none;
  transition: background 0.15s;
  align-self: flex-start;
}
.btn-edit-profile:hover { background: rgba(255,255,255,0.2); }

/* Body */
.profile-body {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--sp-8);
  padding-top: var(--sp-8);
  padding-bottom: var(--sp-8);
}

.section-heading {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  margin: 0 0 var(--sp-5);
  color: var(--color-text);
}
.heading-accent { color: var(--color-danger); font-style: italic; }

/* Course mini-cards */
.course-list { display: flex; flex-direction: column; gap: var(--sp-2); }

.course-mini-card {
  display: flex;
  align-items: center;
  gap: var(--sp-3);
  padding: var(--sp-3);
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  text-decoration: none;
  color: var(--color-text);
  transition: background 0.15s, box-shadow 0.15s;
}
.course-mini-card:hover {
  background: var(--color-muted-bg);
  box-shadow: var(--shadow-sm);
}

.course-mini-cover {
  width: 44px; height: 44px;
  border-radius: var(--radius-sm);
  overflow: hidden;
  flex-shrink: 0;
  background: linear-gradient(135deg, #3498db, #2c3e50);
}
.course-mini-cover img { width: 100%; height: 100%; object-fit: cover; display: block; }
.course-mini-cover-fallback {
  width: 100%; height: 100%;
  display: flex; align-items: center; justify-content: center;
  color: white;
  font-size: var(--font-size-md);
}

.course-mini-info { flex: 1; min-width: 0; }
.course-mini-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  margin-bottom: var(--sp-1);
}
.course-mini-arrow { color: var(--color-muted); font-size: 11px; flex-shrink: 0; }

.empty-courses {
  text-align: center;
  padding: var(--sp-10) var(--sp-4);
  color: var(--color-muted);
  border: 1px dashed var(--color-border);
  border-radius: var(--radius-lg);
}
.empty-icon { font-size: 2rem; margin-bottom: var(--sp-3); display: block; }

/* Data list */
.data-list { margin: 0; }
.data-row {
  display: grid;
  grid-template-columns: 160px 1fr;
  gap: var(--sp-2);
  padding: var(--sp-3) 0;
  border-bottom: 1px solid var(--color-border);
}
.data-row:first-child { border-top: 1px solid var(--color-border); }
.data-label {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  display: flex;
  align-items: center;
  gap: var(--sp-2);
}
.data-label i { width: 14px; text-align: center; color: var(--color-muted); }
.data-value { font-size: var(--font-size-sm); color: var(--color-muted); margin: 0; }

@media (max-width: 768px) {
  .profile-body { grid-template-columns: 1fr; }
  .profile-hero-content { flex-direction: column; text-align: center; }
  .avatar-wrap { margin: 0 auto; }
}
</style>
