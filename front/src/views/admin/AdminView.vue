<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import Swal from 'sweetalert2'
import { getCourses, deleteCourse, getEnrollments, getAvailableStudents, enrollStudent, unenrollStudent } from '../../api/courses'
import { getUsers, toggleUserActive } from '../../api/users'
import { getRoles, register } from '../../api/auth'
import { getAdminStats } from '../../api/admin'
import type { AdminStats } from '../../api/admin'
import type { VistaCursosDetalles, Rol } from '../../types'
import CourseCard from '../../components/CourseCard.vue'
import CourseEditModal from '../../components/CourseEditModal.vue'

const route = useRoute()
const router = useRouter()

type Tab = 'cursos' | 'usuarios' | 'inscripciones'
const activeTab = ref<Tab>((route.query.tab as Tab) ?? 'cursos')
watch(activeTab, tab => router.replace({ query: { ...route.query, tab } }))

// ===== BÚSQUEDA =====
const searchCourses = ref('')
const searchUsers = ref('')

watch(activeTab, () => {
  searchCourses.value = ''
  searchUsers.value = ''
})

// ===== CURSOS =====
const courses = ref<VistaCursosDetalles[]>([])
const loadingCourses = ref(false)
const errorCourses = ref(false)
const editingId = ref<number | null>(null)

const filteredCourses = computed(() => {
  const q = searchCourses.value.trim().toLowerCase()
  if (!q) return courses.value
  return courses.value.filter(c => c.nombreCurso.toLowerCase().includes(q))
})

async function loadCourses() {
  loadingCourses.value = true
  errorCourses.value = false
  try {
    const res = await getCourses()
    courses.value = res.data
  } catch {
    errorCourses.value = true
  } finally {
    loadingCourses.value = false
  }
}

async function onOptions(id: number) {
  const curso = courses.value.find(c => c.idCurso === id)
  const result = await Swal.fire({
    title: curso?.nombreCurso ?? 'Curso',
    text: '¿Qué deseas hacer con este curso?',
    icon: 'question',
    showDenyButton: true,
    showCancelButton: true,
    confirmButtonText: 'Editar',
    denyButtonText: 'Eliminar',
    cancelButtonText: 'Cancelar',
    confirmButtonColor: '#0d6efd',
    denyButtonColor: '#dc3545',
  })
  if (result.isConfirmed) {
    editingId.value = id
  } else if (result.isDenied) {
    const confirm = await Swal.fire({
      title: '¿Eliminar curso?',
      text: '¿Seguro que quieres eliminar este curso? Esta acción no se puede deshacer.',
      icon: 'warning',
      showCancelButton: true,
      confirmButtonText: 'Sí, eliminar',
      cancelButtonText: 'Cancelar',
      confirmButtonColor: '#dc3545',
    })
    if (confirm.isConfirmed) {
      await deleteCourse(id)
      await loadCourses()
    }
  }
}

function onSaved() {
  editingId.value = null
  loadCourses()
}

// ===== USUARIOS =====
const users = ref<any[]>([])
const loadingUsers = ref(false)
const errorUsers = ref(false)

const filteredUsers = computed(() => {
  const q = searchUsers.value.trim().toLowerCase()
  if (!q) return users.value
  return users.value.filter(u =>
    `${u.nombre} ${u.apellidos} ${u.email}`.toLowerCase().includes(q)
  )
})
const roles = ref<Rol[]>([])
const showRegisterPanel = ref(false)
const regForm = ref({ nombre: '', apellidos: '', email: '', password: '', idRol: 0 })
const regMensaje = ref('')
const regError = ref('')
const regSaving = ref(false)

async function loadUsers() {
  loadingUsers.value = true
  errorUsers.value = false
  try {
    const res = await getUsers()
    users.value = res.data
  } catch {
    errorUsers.value = true
  } finally {
    loadingUsers.value = false
  }
}

async function loadRoles() {
  if (roles.value.length) return
  const res = await getRoles()
  roles.value = res.data
  if (roles.value.length && !regForm.value.idRol)
    regForm.value.idRol = roles.value[0].idRol
}

async function handleToggleActive(idUsuario: number) {
  await toggleUserActive(idUsuario)
  await loadUsers()
}

async function submitRegister() {
  regError.value = ''
  regMensaje.value = ''
  regSaving.value = true
  try {
    await register(regForm.value)
    regMensaje.value = `Usuario ${regForm.value.nombre} ${regForm.value.apellidos} registrado correctamente.`
    regForm.value = { nombre: '', apellidos: '', email: '', password: '', idRol: roles.value[0]?.idRol ?? 0 }
    await loadUsers()
  } catch {
    regError.value = 'Error al registrar el usuario. Comprueba que el email no está en uso.'
  } finally {
    regSaving.value = false
  }
}

// ===== INSCRIPCIONES =====
const selectedCourseId = ref<number | null>(null)
const enrollments = ref<any[]>([])
const availableStudents = ref<any[]>([])
const loadingEnrollments = ref(false)
const showEnrollModal = ref(false)
const enrollStudentId = ref(0)
const enrolling = ref(false)

const errorEnrollments = ref(false)

async function loadEnrollments() {
  if (!selectedCourseId.value) return
  loadingEnrollments.value = true
  errorEnrollments.value = false
  try {
    const [enrRes, avRes] = await Promise.all([
      getEnrollments(selectedCourseId.value),
      getAvailableStudents(selectedCourseId.value),
    ])
    enrollments.value = enrRes.data
    availableStudents.value = avRes.data
  } catch {
    errorEnrollments.value = true
  } finally {
    loadingEnrollments.value = false
  }
}

watch(selectedCourseId, loadEnrollments)

async function handleUnenroll(idEstudiante: number, nombre: string) {
  const result = await Swal.fire({
    title: `¿Desinscribir a ${nombre}?`,
    text: 'El estudiante perderá acceso al curso.',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Sí, desinscribir',
    cancelButtonText: 'Cancelar',
    confirmButtonColor: '#dc3545',
  })
  if (!result.isConfirmed) return
  await unenrollStudent(selectedCourseId.value!, idEstudiante)
  await loadEnrollments()
}

async function handleEnroll() {
  if (!enrollStudentId.value || !selectedCourseId.value) return
  enrolling.value = true
  try {
    await enrollStudent(selectedCourseId.value, enrollStudentId.value)
    showEnrollModal.value = false
    enrollStudentId.value = 0
    await loadEnrollments()
  } finally {
    enrolling.value = false
  }
}

// ===== STATS =====
const stats = ref<AdminStats | null>(null)
const loadingStats = ref(true)

async function loadStats() {
  try {
    const res = await getAdminStats()
    stats.value = res.data
  } catch {
    // stats are non-critical; fail silently
  } finally {
    loadingStats.value = false
  }
}

// ===== INIT =====
onMounted(async () => {
  loadStats()
  await loadCourses()
  if (activeTab.value === 'usuarios') { await loadUsers(); await loadRoles() }
  if (activeTab.value === 'inscripciones' && courses.value.length) {
    selectedCourseId.value = courses.value[0].idCurso
  }
})

watch(activeTab, async tab => {
  if (tab === 'usuarios') { await loadUsers(); await loadRoles() }
  if (tab === 'inscripciones' && courses.value.length && !selectedCourseId.value) {
    selectedCourseId.value = courses.value[0].idCurso
  }
})
</script>

<template>
  <div class="page">
    <div class="container">

      <!-- ===== STATS ===== -->
      <div class="stats-section">
        <template v-if="loadingStats">
          <div v-for="i in 4" :key="i" class="stat-card stat-card--skeleton"></div>
        </template>
        <template v-else-if="stats">
          <div class="stat-card">
            <div class="stat-icon stat-icon--blue"><i class="fas fa-graduation-cap"></i></div>
            <div class="stat-body">
              <div class="stat-value">{{ stats.totalCursos }}</div>
              <div class="stat-label">Cursos totales</div>
              <div class="stat-sub">{{ stats.cursosActivos }} activos</div>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon stat-icon--green"><i class="fas fa-users"></i></div>
            <div class="stat-body">
              <div class="stat-value">{{ stats.totalUsuarios }}</div>
              <div class="stat-label">Usuarios</div>
              <div class="stat-sub">{{ stats.totalEstudiantes }} estudiantes · {{ stats.totalProfesores }} profesores</div>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon stat-icon--purple"><i class="fas fa-user-check"></i></div>
            <div class="stat-body">
              <div class="stat-value">{{ stats.totalInscripciones }}</div>
              <div class="stat-label">Inscripciones</div>
              <div class="stat-sub">en todos los cursos</div>
            </div>
          </div>
          <div class="stat-card">
            <div class="stat-icon stat-icon--orange"><i class="fas fa-clock"></i></div>
            <div class="stat-body">
              <div class="stat-value">{{ stats.entregasPendientes }}</div>
              <div class="stat-label">Entregas pendientes</div>
              <div class="stat-sub">sin calificar</div>
            </div>
          </div>
        </template>
      </div>

      <div v-if="stats && stats.actividadReciente.length" class="activity-section">
        <h3 class="activity-title"><i class="fas fa-history"></i> Actividad reciente</h3>
        <div class="activity-list">
          <div v-for="item in stats.actividadReciente" :key="item.idEntrega" class="activity-item">
            <div class="activity-av">{{ item.nombreEstudiante[0] }}</div>
            <div class="activity-body">
              <span class="activity-name">{{ item.nombreEstudiante }}</span>
              <span class="activity-text"> entregó </span>
              <span class="activity-content">{{ item.tituloContenido }}</span>
            </div>
            <div class="activity-meta">
              <span class="badge" :class="item.estado === 'Revisado' ? 'badge-activo' : 'badge-secondary'">
                {{ item.estado }}
              </span>
              <span class="activity-date">{{ new Date(item.fechaEntrega).toLocaleDateString('es-ES') }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Page header -->
      <div class="page-header">
        <div class="page-tabs">
          <span class="page-tab" :class="{ active: activeTab === 'cursos' }" @click="activeTab = 'cursos'">
            <i class="fas fa-graduation-cap"></i> Cursos
          </span>
          <span class="page-tab" :class="{ active: activeTab === 'usuarios' }" @click="activeTab = 'usuarios'">
            <i class="fas fa-users"></i> Usuarios
          </span>
          <span class="page-tab" :class="{ active: activeTab === 'inscripciones' }" @click="activeTab = 'inscripciones'">
            <i class="fas fa-user-plus"></i> Inscripciones
          </span>
        </div>

        <div class="header-actions">
          <RouterLink v-if="activeTab === 'cursos'" to="/admin/cursos/crear" class="btn btn-primary">
            <i class="fas fa-plus"></i> Crear Curso
          </RouterLink>
          <button v-if="activeTab === 'usuarios'" class="btn btn-primary" @click="showRegisterPanel = !showRegisterPanel">
            <i class="fas fa-user-plus"></i> Nuevo Usuario
          </button>
          <button v-if="activeTab === 'inscripciones' && selectedCourseId" class="btn btn-primary" @click="showEnrollModal = true">
            <i class="fas fa-user-plus"></i> Inscribir Estudiante
          </button>
        </div>
      </div>

      <!-- ===== TAB CURSOS ===== -->
      <template v-if="activeTab === 'cursos'">
        <div v-if="loadingCourses" class="loading-center"><div class="spinner"></div></div>
        <template v-else-if="!errorCourses && courses.length">
          <div class="search-bar">
            <i class="fas fa-search search-icon"></i>
            <input v-model="searchCourses" type="text" class="search-input" placeholder="Buscar por nombre del curso…" />
          </div>
          <div v-if="filteredCourses.length" class="courses-grid">
            <CourseCard
              v-for="curso in filteredCourses"
              :key="curso.idCurso"
              :curso="curso"
              :admin-mode="true"
              @options="onOptions"
            />
          </div>
          <div v-else class="empty-state">
            <i class="fas fa-search empty-icon"></i>
            <p>No hay cursos que coincidan con "{{ searchCourses }}".</p>
          </div>
        </template>
        <div v-else-if="errorCourses" class="error-state">
          <i class="fas fa-exclamation-triangle"></i>
          <p>Error al cargar los cursos. Inténtalo de nuevo.</p>
          <button class="btn btn-primary" @click="loadCourses">Reintentar</button>
        </div>
        <div v-else class="empty-state">
          <i class="fas fa-graduation-cap empty-icon"></i>
          <p>Aún no hay cursos creados.</p>
          <RouterLink to="/admin/cursos/crear" class="btn btn-primary">Crear primer curso</RouterLink>
        </div>
      </template>

      <!-- ===== TAB USUARIOS ===== -->
      <template v-else-if="activeTab === 'usuarios'">
        <!-- Register panel -->
        <div v-if="showRegisterPanel" class="register-panel">
          <h3 class="register-title">Nuevo usuario</h3>
          <div class="reg-form-grid">
            <div class="field">
              <label class="form-label">Nombre</label>
              <input v-model="regForm.nombre" type="text" class="form-control" required />
            </div>
            <div class="field">
              <label class="form-label">Apellidos</label>
              <input v-model="regForm.apellidos" type="text" class="form-control" required />
            </div>
            <div class="field">
              <label class="form-label">Email</label>
              <input v-model="regForm.email" type="email" class="form-control" required />
            </div>
            <div class="field">
              <label class="form-label">Contraseña</label>
              <input v-model="regForm.password" type="password" class="form-control" required />
            </div>
          </div>
          <div class="field mt-2">
            <label class="form-label">Rol</label>
            <div class="role-group">
              <button v-for="rol in roles" :key="rol.idRol" type="button" class="role-btn"
                :class="{ active: regForm.idRol === rol.idRol }" @click="regForm.idRol = rol.idRol">
                {{ rol.nombre }}
              </button>
            </div>
          </div>
          <div class="reg-actions">
            <button class="btn btn-outline-secondary" @click="showRegisterPanel = false">Cancelar</button>
            <button class="btn btn-success" @click="submitRegister" :disabled="regSaving">
              <span v-if="regSaving"><i class="fas fa-spinner fa-spin"></i> Registrando…</span>
              <span v-else><i class="fas fa-user-plus"></i> Registrar</span>
            </button>
          </div>
          <p v-if="regMensaje" class="feedback feedback-success">{{ regMensaje }}</p>
          <p v-if="regError" class="feedback feedback-error">{{ regError }}</p>
        </div>

        <div v-if="loadingUsers" class="loading-center"><div class="spinner"></div></div>
        <div v-else-if="errorUsers" class="error-state">
          <i class="fas fa-exclamation-triangle"></i>
          <p>Error al cargar los usuarios. Inténtalo de nuevo.</p>
          <button class="btn btn-primary" @click="loadUsers">Reintentar</button>
        </div>
        <div v-else-if="users.length">
          <div class="search-bar">
            <i class="fas fa-search search-icon"></i>
            <input v-model="searchUsers" type="text" class="search-input" placeholder="Buscar por nombre, apellidos o email…" />
          </div>
          <div v-if="filteredUsers.length" class="users-table-wrap">
          <table class="users-table">
            <thead>
              <tr>
                <th>Usuario</th>
                <th>Email</th>
                <th>Rol</th>
                <th>Estado</th>
                <th>Acción</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="u in filteredUsers" :key="u.idUsuario">
                <td class="user-name-cell">
                  <div class="user-av">{{ u.nombre?.[0] }}</div>
                  <span>{{ u.nombre }} {{ u.apellidos }}</span>
                </td>
                <td>{{ u.email }}</td>
                <td><span class="badge badge-role">{{ u.rol }}</span></td>
                <td>
                  <span class="badge" :class="u.activo ? 'badge-activo' : 'badge-secondary'">
                    {{ u.activo ? 'Activo' : 'Inactivo' }}
                  </span>
                </td>
                <td>
                  <button class="btn btn-sm" :class="u.activo ? 'btn-outline-danger' : 'btn-success'"
                    @click="handleToggleActive(u.idUsuario)">
                    {{ u.activo ? 'Desactivar' : 'Activar' }}
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
          </div>
          <div v-else class="empty-state">
            <i class="fas fa-search empty-icon"></i>
            <p>No hay usuarios que coincidan con "{{ searchUsers }}".</p>
          </div>
        </div>
        <div v-else class="empty-state">
          <i class="fas fa-users empty-icon"></i>
          <p>No hay usuarios registrados.</p>
        </div>
      </template>

      <!-- ===== TAB INSCRIPCIONES ===== -->
      <template v-else-if="activeTab === 'inscripciones'">
        <div class="enroll-filter">
          <label class="form-label">Selecciona un curso</label>
          <select v-model="selectedCourseId" class="form-control enroll-select">
            <option v-for="c in courses" :key="c.idCurso" :value="c.idCurso">{{ c.nombreCurso }}</option>
          </select>
        </div>

        <div v-if="loadingEnrollments" class="loading-center"><div class="spinner"></div></div>
        <div v-else-if="errorEnrollments" class="error-state">
          <i class="fas fa-exclamation-triangle"></i>
          <p>Error al cargar las inscripciones. Inténtalo de nuevo.</p>
          <button class="btn btn-primary" @click="loadEnrollments">Reintentar</button>
        </div>
        <div v-else-if="selectedCourseId">
          <div v-if="enrollments.length" class="users-table-wrap">
            <table class="users-table">
              <thead>
                <tr>
                  <th>Estudiante</th>
                  <th>Email</th>
                  <th>Fecha inscripción</th>
                  <th>Progreso</th>
                  <th>Acción</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="e in enrollments" :key="e.idInscripcion">
                  <td class="user-name-cell">
                    <div class="user-av">{{ e.nombreEstudiante?.[0] }}</div>
                    <span>{{ e.nombreEstudiante }}</span>
                  </td>
                  <td>{{ e.email }}</td>
                  <td>{{ e.fechaInscripcion ? new Date(e.fechaInscripcion).toLocaleDateString('es-ES') : '—' }}</td>
                  <td>
                    <div class="progress-wrap">
                      <div class="progress-bar">
                        <div class="progress-fill" :style="{ width: `${e.porcentajeCompletado ?? 0}%` }"></div>
                      </div>
                      <span class="progress-text">{{ Math.round(e.porcentajeCompletado ?? 0) }}%</span>
                    </div>
                  </td>
                  <td>
                    <button class="btn btn-sm btn-outline-danger" @click="handleUnenroll(e.idEstudiante, e.nombreEstudiante)">
                      Desinscribir
                    </button>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-else class="empty-state">
            <i class="fas fa-user-slash empty-icon"></i>
            <p>No hay estudiantes inscritos en este curso.</p>
          </div>
        </div>
      </template>

    </div>
  </div>

  <CourseEditModal :id-curso="editingId" @saved="onSaved" @close="editingId = null" />

  <!-- Modal inscribir estudiante -->
  <Teleport to="body">
    <div v-if="showEnrollModal" class="modal-overlay" @mousedown.self="showEnrollModal = false">
      <div class="modal-dialog">
        <div class="modal-header">
          <h5 class="modal-title"><i class="fas fa-user-plus"></i> Inscribir Estudiante</h5>
          <button class="modal-close" @click="showEnrollModal = false"><i class="fas fa-times"></i></button>
        </div>
        <div class="modal-body">
          <div class="field">
            <label class="form-label">Selecciona un estudiante</label>
            <select v-model="enrollStudentId" class="form-control">
              <option value="0" disabled>— Elige un estudiante —</option>
              <option v-for="s in availableStudents" :key="s.idUsuario" :value="s.idUsuario">
                {{ s.nombre }} {{ s.apellidos }} ({{ s.email }})
              </option>
            </select>
          </div>
          <p v-if="!availableStudents.length" class="text-muted">Todos los estudiantes ya están inscritos en este curso.</p>
        </div>
        <div class="modal-footer">
          <button class="btn btn-outline-secondary" @click="showEnrollModal = false">Cancelar</button>
          <button class="btn btn-primary" :disabled="!enrollStudentId || enrolling" @click="handleEnroll">
            <span v-if="enrolling"><i class="fas fa-spinner fa-spin"></i> Inscribiendo…</span>
            <span v-else>Inscribir</span>
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-bottom: 2px solid var(--color-border);
  margin-bottom: var(--sp-6);
  flex-wrap: wrap;
  gap: var(--sp-3);
}

.page-tabs {
  display: flex;
  gap: 0;
  margin-bottom: -2px;
}

/* En móvil, si las tabs no caben, scroll horizontal interno con fade derecho
   en lugar de propagar el overflow a toda la página. */
@media (max-width: 768px) {
  .page-tabs {
    overflow-x: auto;
    scrollbar-width: none;
    -ms-overflow-style: none;
    -webkit-mask-image: linear-gradient(to right, black 88%, transparent);
            mask-image: linear-gradient(to right, black 88%, transparent);
  }
  .page-tabs::-webkit-scrollbar { display: none; }
}

.page-tab {
  display: inline-flex;
  align-items: center;
  gap: var(--sp-2);
  padding: var(--sp-3) var(--sp-5);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-muted);
  border-bottom: 2px solid transparent;
  cursor: pointer;
  user-select: none;
  white-space: nowrap;
  flex-shrink: 0;
  transition: color 0.15s, border-color 0.15s;
}
.page-tab.active {
  color: var(--color-text);
  font-weight: var(--font-weight-semibold);
  border-bottom-color: var(--color-black);
}

.header-actions { display: flex; gap: var(--sp-2); }

.search-bar {
  position: relative;
  margin-bottom: var(--sp-5);
  max-width: 400px;
}
.search-icon {
  position: absolute;
  left: var(--sp-3);
  top: 50%;
  transform: translateY(-50%);
  color: var(--color-muted);
  font-size: var(--font-size-sm);
  pointer-events: none;
}
.search-input {
  width: 100%;
  padding: var(--sp-2) var(--sp-3) var(--sp-2) var(--sp-8);
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  color: var(--color-text);
  background: var(--color-white);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  outline: none;
  transition: border-color 0.15s, box-shadow 0.15s;
}
.search-input:focus {
  border-color: #86b7fe;
  box-shadow: 0 0 0 3px rgba(13, 110, 253, 0.15);
}
.search-input::placeholder { color: #adb5bd; }

.courses-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: var(--sp-6);
}

.loading-center { display: flex; justify-content: center; padding: var(--sp-12) 0; }

.empty-state {
  display: flex; flex-direction: column; align-items: center;
  gap: var(--sp-4); padding: var(--sp-16) 0; color: var(--color-muted); text-align: center;
}
.empty-icon { font-size: 3rem; opacity: 0.3; }
.empty-state p { margin: 0; font-size: var(--font-size-lg); }

.error-state {
  display: flex; flex-direction: column; align-items: center;
  gap: var(--sp-4); padding: var(--sp-12) 0; color: #dc2626; text-align: center;
}
.error-state i { font-size: 2.5rem; opacity: 0.7; }
.error-state p { margin: 0; font-size: var(--font-size-md); color: var(--color-text); }

/* Register panel */
.register-panel {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--sp-6);
  margin-bottom: var(--sp-6);
}
.register-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  margin: 0 0 var(--sp-5);
}
.reg-form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--sp-4);
}
.field { display: flex; flex-direction: column; gap: var(--sp-1); }
.mt-2 { margin-top: var(--sp-2); }
.role-group { display: flex; flex-wrap: wrap; gap: var(--sp-2); }
.role-btn {
  padding: var(--sp-2) var(--sp-4);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  background: var(--color-surface);
  color: var(--color-text);
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s;
}
.role-btn.active { background: var(--color-primary); border-color: var(--color-primary); color: white; }
.reg-actions { display: flex; gap: var(--sp-3); justify-content: flex-end; margin-top: var(--sp-5); }
.feedback { margin-top: var(--sp-3); font-weight: var(--font-weight-semibold); font-size: var(--font-size-sm); }
.feedback-success { color: var(--color-success); }
.feedback-error { color: var(--color-danger); }

/* Users table */
.users-table-wrap {
  overflow-x: auto;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
}
.users-table {
  width: 100%;
  border-collapse: collapse;
  font-size: var(--font-size-sm);
}
.users-table th {
  background: var(--color-muted-bg);
  padding: var(--sp-3) var(--sp-4);
  text-align: left;
  font-weight: var(--font-weight-semibold);
  border-bottom: 1px solid var(--color-border);
}
.users-table td {
  padding: var(--sp-3) var(--sp-4);
  border-bottom: 1px solid var(--color-border);
  vertical-align: middle;
}
.users-table tr:last-child td { border-bottom: none; }
.users-table tr:hover td { background: var(--color-muted-bg); }

.user-name-cell { display: flex; align-items: center; gap: var(--sp-3); }
.user-av {
  width: 32px; height: 32px; border-radius: 50%;
  background: var(--color-primary); color: white;
  display: flex; align-items: center; justify-content: center;
  font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold);
  flex-shrink: 0; text-transform: uppercase;
}

.badge-role { background: #e0e7ff; color: #4338ca; }

/* Enrollments */
.enroll-filter { margin-bottom: var(--sp-5); display: flex; flex-direction: column; gap: var(--sp-1); max-width: 400px; }
.enroll-select { max-width: 400px; }

.progress-wrap { display: flex; align-items: center; gap: var(--sp-2); }
.progress-bar { flex: 1; height: 8px; background: var(--color-border); border-radius: 999px; overflow: hidden; }
.progress-fill { height: 100%; background: var(--color-primary); border-radius: 999px; transition: width 0.3s; }
.progress-text { font-size: var(--font-size-xs); color: var(--color-muted); white-space: nowrap; }

.text-muted { color: var(--color-muted); font-size: var(--font-size-sm); }

/* Modal */
.modal-header { display: flex; align-items: center; justify-content: space-between; padding: var(--sp-5) var(--sp-6); border-bottom: 1px solid var(--color-border); }
.modal-title { font-size: var(--font-size-lg); font-weight: var(--font-weight-semibold); margin: 0; display: flex; align-items: center; gap: var(--sp-2); }
.modal-close { background: none; border: none; font-size: var(--font-size-md); cursor: pointer; color: var(--color-muted); padding: var(--sp-1); }
.modal-body { padding: var(--sp-5) var(--sp-6); }
.modal-footer { display: flex; justify-content: flex-end; gap: var(--sp-3); padding: var(--sp-4) var(--sp-6); border-top: 1px solid var(--color-border); }

@media (max-width: 640px) {
  .reg-form-grid { grid-template-columns: 1fr; }
}

/* Stats */
.stats-section {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
  gap: var(--sp-4);
  margin-bottom: var(--sp-5);
}

.stat-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--sp-4) var(--sp-5);
  display: flex;
  align-items: center;
  gap: var(--sp-4);
  box-shadow: var(--shadow-sm);
}

.stat-card--skeleton {
  min-height: 80px;
  background: linear-gradient(90deg, var(--color-muted-bg) 25%, var(--color-border) 50%, var(--color-muted-bg) 75%);
  background-size: 200% 100%;
  animation: shimmer 1.5s infinite;
}

@keyframes shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}

.stat-icon {
  width: 44px; height: 44px;
  border-radius: var(--radius-md);
  display: flex; align-items: center; justify-content: center;
  font-size: var(--font-size-xl);
  flex-shrink: 0;
}
.stat-icon--blue { background: #dbeafe; color: #2563eb; }
.stat-icon--green { background: #dcfce7; color: #16a34a; }
.stat-icon--purple { background: #ede9fe; color: #7c3aed; }
.stat-icon--orange { background: #ffedd5; color: #d97706; }

.stat-body { display: flex; flex-direction: column; gap: 2px; min-width: 0; }
.stat-value { font-size: 1.75rem; font-weight: var(--font-weight-bold); color: var(--color-text); line-height: 1; }
.stat-label { font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); color: var(--color-text); }
.stat-sub { font-size: var(--font-size-xs); color: var(--color-muted); }

/* Activity */
.activity-section {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--sp-5) var(--sp-6);
  margin-bottom: var(--sp-6);
  box-shadow: var(--shadow-sm);
}
.activity-title {
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0 0 var(--sp-4);
  display: flex; align-items: center; gap: var(--sp-2);
}
.activity-list { display: flex; flex-direction: column; gap: var(--sp-3); }
.activity-item {
  display: flex; align-items: center; gap: var(--sp-3);
  padding: var(--sp-3) 0;
  border-bottom: 1px solid var(--color-border);
}
.activity-item:last-child { border-bottom: none; padding-bottom: 0; }
.activity-av {
  width: 34px; height: 34px; border-radius: 50%;
  background: var(--color-primary); color: #fff;
  display: flex; align-items: center; justify-content: center;
  font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold);
  flex-shrink: 0; text-transform: uppercase;
}
.activity-body { flex: 1; min-width: 0; font-size: var(--font-size-sm); color: var(--color-text); }
.activity-name { font-weight: var(--font-weight-semibold); }
.activity-text { color: var(--color-muted); }
.activity-content { color: var(--color-primary); }
.activity-meta { display: flex; flex-direction: column; align-items: flex-end; gap: var(--sp-1); flex-shrink: 0; }
.activity-date { font-size: var(--font-size-xs); color: var(--color-muted); }
</style>
