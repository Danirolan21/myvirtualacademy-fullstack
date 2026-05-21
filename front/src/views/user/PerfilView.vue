<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue'
import { useAuthStore } from '../../stores/auth'
import { getUser, updateUser } from '../../api/users'
import { getCoursesByProfessor } from '../../api/courses'
import { getSubjectsByStudent } from '../../api/subjects'
import { formatDate } from '../../utils/format'
import type { Usuario, VistaCursosDetalles, AsignaturaUsuarioDTO } from '../../types'
import { userAvatar, courseCover } from '../../utils/images'

const auth = useAuthStore()

// ─── State ────────────────────────────────────────────────────────────────────
const usuario = ref<Usuario | null>(null)
const loading = ref(true)
const editing = ref(false)
const saving = ref(false)

// Edit form (copy of user data)
const editForm = reactive({ nombre: '', apellidos: '', telefono: '' })

// Avatar
const avatarFile = ref<File | null>(null)
const avatarPreview = ref<string | null>(null)

// Password change
const pwForm = reactive({ actual: '', nueva: '', confirmar: '' })
const pwError = ref('')
const saveSuccess = ref(false)
let successTimer: ReturnType<typeof setTimeout> | null = null

// Courses (view mode)
interface CourseItem {
  idCurso: number; nombreCurso: string; estado: string
  imagenPortada?: string; linkTo?: string
}
const cursos = ref<CourseItem[]>([])

const statusClass: Record<string, string> = {
  Activo: 'badge-activo', Borrador: 'badge-borrador',
  Finalizado: 'badge-finalizado', Archivado: 'badge-archivado', Suspendido: 'badge-suspendido',
}

// ─── Load ─────────────────────────────────────────────────────────────────────
onMounted(async () => {
  try {
    const res = await getUser(auth.user!.id)
    usuario.value = res.data

    const role = auth.role
    const userId = auth.user!.id

    if (auth.isAdmin || role === 'Administrador') {
      try {
        const r = await getCoursesByProfessor(userId)
        cursos.value = r.data.map((c: VistaCursosDetalles) => ({
          idCurso: c.idCurso, nombreCurso: c.nombreCurso,
          estado: c.estado, imagenPortada: c.imagenPortada, linkTo: `/admin/cursos/${c.idCurso}`,
        }))
      } catch { /* admin puede no tener cursos */ }
    } else if (role === 'Profesor' || role === 'Tutor') {
      const r = await getCoursesByProfessor(userId)
      cursos.value = r.data.map((c: VistaCursosDetalles) => ({
        idCurso: c.idCurso, nombreCurso: c.nombreCurso,
        estado: c.estado, imagenPortada: c.imagenPortada, linkTo: '/profesor',
      }))
    } else {
      const r = await getSubjectsByStudent(userId)
      const seen = new Set<number>()
      cursos.value = r.data
        .filter((a: AsignaturaUsuarioDTO) => { if (seen.has(a.idCurso)) return false; seen.add(a.idCurso); return true })
        .map((a: AsignaturaUsuarioDTO) => ({
          idCurso: a.idCurso, nombreCurso: a.nombreCurso, estado: a.estado, linkTo: '/estudiante',
        }))
    }
  } finally {
    loading.value = false
  }
})

// ─── Edit mode ────────────────────────────────────────────────────────────────
function startEdit() {
  if (!usuario.value) return
  editForm.nombre = usuario.value.nombre
  editForm.apellidos = usuario.value.apellidos
  editForm.telefono = usuario.value.telefono ?? ''
  pwForm.actual = ''; pwForm.nueva = ''; pwForm.confirmar = ''
  pwError.value = ''; saveSuccess.value = false
  if (successTimer) { clearTimeout(successTimer); successTimer = null }
  avatarFile.value = null; avatarPreview.value = null
  editing.value = true
}

function cancelEdit() {
  editing.value = false
}

function onAvatarChange(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0] ?? null
  avatarFile.value = file
  if (file) avatarPreview.value = URL.createObjectURL(file)
}

async function submit() {
  pwError.value = ''
  saveSuccess.value = false

  // Password validation
  const anyPw = pwForm.actual || pwForm.nueva || pwForm.confirmar
  if (anyPw) {
    if (!pwForm.actual || !pwForm.nueva || !pwForm.confirmar)
      return (pwError.value = 'Rellena los tres campos de contraseña.')
    if (pwForm.nueva.length < 6)
      return (pwError.value = 'La nueva contraseña debe tener al menos 6 caracteres.')
    if (pwForm.nueva !== pwForm.confirmar)
      return (pwError.value = 'La nueva contraseña y su confirmación no coinciden.')
  }

  saving.value = true
  try {
    const fd = new FormData()
    fd.append('nombre', editForm.nombre)
    fd.append('apellidos', editForm.apellidos)
    fd.append('telefono', editForm.telefono)
    if (avatarFile.value) fd.append('fotoPerfil', avatarFile.value)
    if (anyPw) {
      fd.append('contrasenaActual', pwForm.actual)
      fd.append('nuevaContrasena', pwForm.nueva)
    }

    const res = await updateUser(auth.user!.id, fd)

    // Sync local state
    if (usuario.value) {
      usuario.value.nombre = editForm.nombre
      usuario.value.apellidos = editForm.apellidos
      usuario.value.telefono = editForm.telefono
      if (res.data?.fotoPerfil) {
        auth.user!.fotoPerfil = res.data.fotoPerfil
        avatarPreview.value = null
      }
    }
    if (anyPw) { pwForm.actual = ''; pwForm.nueva = ''; pwForm.confirmar = '' }

    saveSuccess.value = true
    successTimer = setTimeout(() => { saveSuccess.value = false }, 3000)
    editing.value = false
  } catch (err: any) {
    pwError.value = err?.response?.data?.message ?? 'Error al guardar los cambios.'
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <div>
    <div v-if="loading" class="loading-center"><div class="spinner"></div></div>

    <template v-else-if="usuario">

      <!-- ===== HERO ===== -->
      <div class="profile-hero">
        <div class="profile-hero-bg"></div>
        <div class="profile-hero-content container">

          <!-- Avatar -->
          <div class="avatar-wrap">
            <img
              :src="(editing && avatarPreview) ? avatarPreview : userAvatar(auth.user?.fotoPerfil)"
              class="avatar-img"
              alt="Avatar"
            />
            <label v-if="editing" class="avatar-edit-btn" for="avatar-upload" title="Cambiar foto">
              <i class="fas fa-pencil-alt"></i>
            </label>
            <input v-if="editing" id="avatar-upload" type="file" accept="image/*"
              class="visually-hidden" @change="onAvatarChange" />
            <button v-else class="avatar-edit-btn" type="button" @click="startEdit" title="Editar perfil">
              <i class="fas fa-pencil-alt"></i>
            </button>
          </div>

          <!-- Name — always static in hero -->
          <div class="profile-hero-info">
            <h1 class="profile-name">{{ usuario.nombre }} {{ usuario.apellidos }}</h1>
            <span class="profile-role-badge">{{ auth.role }}</span>
          </div>

          <!-- Edit button (view mode only) -->
          <button v-if="!editing" class="btn-edit-profile" type="button" @click="startEdit">
            <i class="fas fa-user-edit"></i> Editar perfil
          </button>

        </div>
      </div>

      <!-- ===== VIEW MODE BODY ===== -->
      <div v-if="!editing" class="container profile-body">

        <!-- Left: Mis Cursos -->
        <section class="profile-section">
          <h2 class="section-heading">Mis <span class="heading-accent">Cursos</span></h2>
          <div v-if="cursos.length" class="course-list">
            <RouterLink v-for="c in cursos" :key="c.idCurso" :to="c.linkTo ?? '/'" class="course-mini-card">
              <div class="course-mini-cover">
                <img v-if="c.imagenPortada" :src="courseCover(c.imagenPortada)" :alt="c.nombreCurso" />
                <div v-else class="course-mini-cover-fallback"><i class="fas fa-graduation-cap"></i></div>
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
          <h2 class="section-heading">Más <span class="heading-accent">datos</span></h2>
          <dl class="data-list">
            <div class="data-row">
              <dt class="data-label"><i class="fas fa-envelope"></i> Email</dt>
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
            <div v-if="usuario.ultimoAcceso" class="data-row">
              <dt class="data-label"><i class="fas fa-clock"></i> Último acceso</dt>
              <dd class="data-value">{{ formatDate(usuario.ultimoAcceso) }}</dd>
            </div>
          </dl>
        </section>

      </div>

      <!-- ===== EDIT MODE BODY ===== -->
      <form v-else class="container edit-body" @submit.prevent="submit" novalidate>

        <!-- Left: datos personales -->
        <section class="edit-section">
          <h2 class="section-heading">Datos <span class="heading-accent">personales</span></h2>

          <div class="field">
            <label class="form-label">Nombre</label>
            <input v-model="editForm.nombre" type="text" class="form-control" required />
          </div>
          <div class="field">
            <label class="form-label">Apellidos</label>
            <input v-model="editForm.apellidos" type="text" class="form-control" required />
          </div>
          <div class="field">
            <label class="form-label">Teléfono</label>
            <input v-model="editForm.telefono" type="text" class="form-control" placeholder="Número de teléfono" />
          </div>
        </section>

        <!-- Right: cambio de contraseña -->
        <section class="edit-section">
          <h2 class="section-heading">Cambiar <span class="heading-accent">contraseña</span></h2>
          <p class="pw-hint">Deja los campos vacíos si no quieres cambiar la contraseña.</p>

          <div class="field">
            <label class="form-label">Contraseña actual</label>
            <input v-model="pwForm.actual" type="password" class="form-control" autocomplete="current-password" />
          </div>
          <div class="field">
            <label class="form-label">Nueva contraseña</label>
            <input v-model="pwForm.nueva" type="password" class="form-control" autocomplete="new-password"
              placeholder="Mínimo 6 caracteres" />
          </div>
          <div class="field">
            <label class="form-label">Confirmar nueva contraseña</label>
            <input v-model="pwForm.confirmar" type="password" class="form-control" autocomplete="new-password" />
          </div>

        </section>

        <!-- Bottom actions (spans both columns) -->
        <div class="edit-actions">
          <button type="button" class="btn btn-outline-secondary" @click="cancelEdit" :disabled="saving">
            <i class="fas fa-times"></i> Cancelar
          </button>
          <button type="submit" class="btn btn-primary" :disabled="saving">
            <span v-if="saving"><i class="fas fa-spinner fa-spin"></i> Guardando…</span>
            <span v-else><i class="fas fa-save"></i> Guardar cambios</span>
          </button>
        </div>
        <p v-if="pwError" class="feedback feedback-error actions-feedback">
          <i class="fas fa-exclamation-circle"></i> {{ pwError }}
        </p>
        <p v-if="saveSuccess" class="feedback feedback-success actions-feedback">
          <i class="fas fa-check-circle"></i> Cambios guardados correctamente
        </p>

      </form>

    </template>
  </div>
</template>

<style scoped>
.loading-center { display: flex; justify-content: center; padding: var(--sp-12) 0; }
.visually-hidden { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0,0,0,0); white-space: nowrap; }

/* ─── Hero ─────────────────────────────────────────────────────────────────── */
.profile-hero {
  position: relative;
  background: #1a1a2e;
  overflow: hidden;
  padding: var(--sp-10) 0 var(--sp-8);
}
.profile-hero-bg {
  position: absolute; inset: 0;
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
  position: absolute; bottom: 4px; right: 4px;
  width: 28px; height: 28px;
  background: var(--color-primary); color: white;
  border-radius: 50%; border: none;
  display: flex; align-items: center; justify-content: center;
  font-size: 11px; cursor: pointer; text-decoration: none;
  transition: background 0.15s;
}
.avatar-edit-btn:hover { background: #0b5ed7; }

.profile-hero-info { flex: 1; display: flex; flex-direction: column; gap: var(--sp-2); }
.profile-name {
  font-size: var(--font-size-2xl); font-weight: var(--font-weight-bold);
  color: white; margin: 0;
}
.profile-role-badge {
  display: inline-block; width: fit-content;
  background: rgba(255,255,255,0.15); color: rgba(255,255,255,0.9);
  border: 1px solid rgba(255,255,255,0.25); border-radius: 999px;
  padding: var(--sp-1) var(--sp-3);
  font-size: var(--font-size-sm); font-weight: var(--font-weight-medium);
}

.hero-input {
  background: transparent; border: none;
  border-bottom: 1px solid rgba(255,255,255,0.4);
  color: white; font-family: var(--font-sans);
  font-size: var(--font-size-2xl); font-weight: var(--font-weight-bold);
  padding: var(--sp-1) 0; width: 100%; outline: none;
  transition: border-color 0.15s;
}
.hero-input:focus { border-bottom-color: rgba(255,255,255,0.9); }
.hero-input::placeholder { color: rgba(255,255,255,0.4); }
.hero-input-sub { font-size: var(--font-size-xl); font-weight: var(--font-weight-semibold); }

.btn-edit-profile {
  display: inline-flex; align-items: center; gap: var(--sp-2);
  padding: var(--sp-2) var(--sp-4);
  background: rgba(255,255,255,0.1); border: 1px solid rgba(255,255,255,0.3);
  border-radius: var(--radius-md); color: white;
  font-family: var(--font-sans); font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium); cursor: pointer;
  text-decoration: none; transition: background 0.15s; align-self: flex-start;
}
.btn-edit-profile:hover { background: rgba(255,255,255,0.2); }

/* ─── View mode body ───────────────────────────────────────────────────────── */
.profile-body {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--sp-8);
  padding-top: var(--sp-8);
  padding-bottom: var(--sp-8);
}

.section-heading {
  font-size: var(--font-size-2xl); font-weight: var(--font-weight-bold);
  margin: 0 0 var(--sp-5); color: var(--color-text);
}
.heading-accent { color: var(--color-danger); font-style: italic; }

.course-list { display: flex; flex-direction: column; gap: var(--sp-2); }
.course-mini-card {
  display: flex; align-items: center; gap: var(--sp-3);
  padding: var(--sp-3); background: var(--color-surface);
  border: 1px solid var(--color-border); border-radius: var(--radius-md);
  text-decoration: none; color: var(--color-text);
  transition: background 0.15s, box-shadow 0.15s;
}
.course-mini-card:hover { background: var(--color-muted-bg); box-shadow: var(--shadow-sm); }
.course-mini-cover {
  width: 44px; height: 44px; border-radius: var(--radius-sm); overflow: hidden;
  flex-shrink: 0; background: linear-gradient(135deg, #3498db, #2c3e50);
}
.course-mini-cover img { width: 100%; height: 100%; object-fit: cover; display: block; }
.course-mini-cover-fallback {
  width: 100%; height: 100%;
  display: flex; align-items: center; justify-content: center;
  color: white; font-size: var(--font-size-md);
}
.course-mini-info { flex: 1; min-width: 0; }
.course-mini-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  margin-bottom: var(--sp-1);
  /* Hasta 2 líneas con ellipsis si excede. Nombres largos como
     "Master Desarrollo Web FullStack + MultiCloud" caben enteros en móvil. */
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
  word-break: break-word;
  overflow-wrap: anywhere;
}
.course-mini-arrow { color: var(--color-muted); font-size: 11px; flex-shrink: 0; }
.empty-courses {
  text-align: center; padding: var(--sp-10) var(--sp-4);
  color: var(--color-muted); border: 1px dashed var(--color-border); border-radius: var(--radius-lg);
}
.empty-icon { font-size: 2rem; margin-bottom: var(--sp-3); display: block; }

.data-list { margin: 0; }
.data-row {
  display: grid; grid-template-columns: 160px minmax(0, 1fr); gap: var(--sp-2);
  padding: var(--sp-3) 0; border-bottom: 1px solid var(--color-border);
}
.data-row:first-child { border-top: 1px solid var(--color-border); }
.data-label {
  font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold);
  color: var(--color-text); display: flex; align-items: center; gap: var(--sp-2);
}
.data-label i { width: 14px; text-align: center; color: var(--color-muted); }
.data-value {
  font-size: var(--font-size-sm); color: var(--color-muted); margin: 0;
  /* Permite romper palabras largas sin espacios (ej. emails) en móvil para
     que no fuercen el ancho de la columna y desborden el viewport. */
  overflow-wrap: anywhere;
  word-break: break-word;
  min-width: 0;
}

/* ─── Edit mode body ───────────────────────────────────────────────────────── */
.edit-body {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--sp-8);
  padding-top: var(--sp-8);
  padding-bottom: var(--sp-8);
  align-items: start;
}

.edit-section { display: flex; flex-direction: column; gap: var(--sp-4); }

.field { display: flex; flex-direction: column; gap: var(--sp-1); }
.field-hint { font-size: var(--font-size-xs); color: var(--color-muted); margin-top: 2px; }

.pw-hint { font-size: var(--font-size-sm); color: var(--color-muted); margin: 0 0 var(--sp-2); }

.feedback { font-size: var(--font-size-sm); display: flex; align-items: center; gap: var(--sp-2); margin: var(--sp-2) 0 0; }
.feedback-error { color: var(--color-danger); }
.feedback-success { color: #16a34a; }

/* Actions row — spans both columns */
.actions-feedback {
  grid-column: 1 / -1;
  justify-content: center;
  margin: var(--sp-1) 0 0;
}

.edit-actions {
  grid-column: 1 / -1;
  display: flex;
  justify-content: center;
  gap: var(--sp-3);
  padding-top: var(--sp-4);
  border-top: 1px solid var(--color-border);
}

/* ─── Responsive ────────────────────────────────────────────────────────────── */
@media (max-width: 768px) {
  .profile-body, .edit-body { grid-template-columns: 1fr; }
  .profile-hero-content { flex-direction: column; text-align: center; }
  .avatar-wrap { margin: 0 auto; }
  .hero-input { text-align: center; }
  .edit-actions { flex-direction: column; }
}
</style>
