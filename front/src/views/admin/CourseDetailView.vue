<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { getCourse, getEnrollments } from '../../api/courses'
import { createSubject, deleteSubject } from '../../api/subjects'
import client from '../../api/client'
import Swal from 'sweetalert2'
import { formatDate } from '../../utils/format'
import type { CourseDetailResponse, Inscripcion } from '../../types'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const curso = ref<CourseDetailResponse | null>(null)
const loading = ref(true)
const error = ref(false)
const activeTab = ref<'asignaturas' | 'alumnos' | 'evaluacion' | 'configuracion'>('asignaturas')

// Lista de profesores para el select de nueva asignatura
interface ProfesorItem { idUsuario: number; nombreCompleto: string }
const profesores = ref<ProfesorItem[]>([])
async function loadProfesores() {
  try {
    const res = await client.get<ProfesorItem[]>('/api/users/profesores')
    profesores.value = res.data
  } catch { /* no bloquea el resto de la vista */ }
}

// Nueva asignatura
const showNewSubjectForm = ref(false)
const newSubjectName = ref('')
const newSubjectProfesor = ref<number | ''>('')
const savingSubject = ref(false)
const subjectError = ref('')

async function submitNewSubject() {
  if (!newSubjectName.value.trim()) return
  savingSubject.value = true
  subjectError.value = ''
  try {
    const idProfesor = newSubjectProfesor.value !== '' ? Number(newSubjectProfesor.value) : undefined
    const res = await createSubject(Number(route.params.id), newSubjectName.value.trim(), idProfesor)
    router.push(`/asignatura/${res.data.idAsignatura}`)
  } catch (e: any) {
    subjectError.value = e?.response?.data?.message ?? 'Error al crear la asignatura.'
  } finally {
    savingSubject.value = false
  }
}

async function confirmDeleteSubject(idAsignatura: number, nombre: string) {
  const result = await Swal.fire({
    title: `¿Eliminar "${nombre}"?`,
    text: 'Se eliminarán todos sus módulos y contenidos. Esta acción no se puede deshacer.',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Sí, eliminar',
    cancelButtonText: 'Cancelar',
    confirmButtonColor: '#dc3545',
  })
  if (!result.isConfirmed) return
  await deleteSubject(idAsignatura)
  curso.value!.asignaturas = (curso.value!.asignaturas ?? []).filter(a => a.idAsignatura !== idAsignatura)
}

const enrollments = ref<Inscripcion[]>([])
const loadingEnrollments = ref(false)
const errorEnrollments = ref(false)

async function load() {
  loading.value = true
  error.value = false
  try {
    const res = await getCourse(Number(route.params.id))
    curso.value = res.data
  } catch {
    error.value = true
  } finally {
    loading.value = false
  }
}

async function loadEnrollments() {
  loadingEnrollments.value = true
  errorEnrollments.value = false
  try {
    const res = await getEnrollments(Number(route.params.id))
    enrollments.value = res.data
  } catch {
    errorEnrollments.value = true
  } finally {
    loadingEnrollments.value = false
  }
}

onMounted(load)

watch(activeTab, tab => {
  if (tab === 'alumnos' && !enrollments.value.length && !loadingEnrollments.value) {
    loadEnrollments()
  }
})

const statusClass: Record<string, string> = {
  Activo: 'badge-activo', Borrador: 'badge-borrador',
  Finalizado: 'badge-finalizado', Archivado: 'badge-archivado', Suspendido: 'badge-suspendido',
}
</script>

<template>
  <div class="page">
    <div class="container">

      <div v-if="loading" class="loading-center"><div class="spinner"></div></div>

      <div v-else-if="error" class="error-state">
        <i class="fas fa-exclamation-triangle"></i>
        <p>Error al cargar los datos del curso. Inténtalo de nuevo.</p>
        <button class="btn btn-primary" @click="load">Reintentar</button>
      </div>

      <template v-else-if="curso">
        <!-- Back link -->
        <RouterLink to="/admin" class="back-link">
          <i class="fas fa-arrow-left"></i> Volver a cursos
        </RouterLink>

        <!-- Header card -->
        <div class="header-card">
          <div class="header-main">
            <span class="badge" :class="statusClass[curso.curso?.estado ?? 'Borrador']">
              {{ curso.curso?.estado ?? 'Borrador' }}
            </span>
            <h1 class="header-title">{{ curso.curso?.nombreCurso ?? (curso as any).nombreCurso }}</h1>
          </div>
          <div class="header-meta">
            <span><i class="fas fa-user"></i> {{ curso.curso?.nombreProfesor ?? (curso as any).nombreProfesor }}</span>
            <span><i class="fas fa-calendar-alt"></i> Inicio: {{ formatDate(curso.curso?.fechaInicio ?? (curso as any).fechaInicio) }}</span>
            <span><i class="fas fa-book-open"></i> Asignaturas: {{ curso.asignaturas?.length ?? 0 }}</span>
            <span><i class="fas fa-users"></i> Alumnos: {{ curso.curso?.numeroAlumnos ?? 0 }}</span>
          </div>
        </div>

        <!-- Tabs -->
        <div class="tabs-bar">
          <button class="tab-btn" :class="{ active: activeTab === 'asignaturas' }" @click="activeTab = 'asignaturas'">Asignaturas</button>
          <button class="tab-btn" :class="{ active: activeTab === 'alumnos' }" @click="activeTab = 'alumnos'">Alumnos</button>
          <button class="tab-btn" :class="{ active: activeTab === 'evaluacion' }" @click="activeTab = 'evaluacion'">Evaluación</button>
          <button class="tab-btn" :class="{ active: activeTab === 'configuracion' }" @click="activeTab = 'configuracion'">Configuración</button>
        </div>

        <!-- Tab: Asignaturas -->
        <div v-if="activeTab === 'asignaturas'" class="tab-panel">
          <!-- Cabecera con botón nueva asignatura -->
          <div v-if="auth.isAdmin" class="subjects-toolbar">
            <span class="subjects-count">{{ curso.asignaturas?.length ?? 0 }} asignatura{{ (curso.asignaturas?.length ?? 0) !== 1 ? 's' : '' }}</span>
            <button class="btn btn-primary btn-sm" @click="showNewSubjectForm = !showNewSubjectForm; if (showNewSubjectForm) loadProfesores()">
              <i class="fas fa-plus"></i> Nueva asignatura
            </button>
          </div>

          <!-- Formulario inline nueva asignatura -->
          <div v-if="showNewSubjectForm" class="new-subject-form">
            <input
              v-model="newSubjectName"
              type="text"
              class="form-control"
              placeholder="Nombre de la asignatura"
              autofocus
              @keydown.enter.prevent="submitNewSubject"
              @keydown.escape="showNewSubjectForm = false"
            />
            <select v-model="newSubjectProfesor" class="form-control">
              <option value="">— Sin profesor asignado —</option>
              <option v-for="p in profesores" :key="p.idUsuario" :value="p.idUsuario">{{ p.nombreCompleto }}</option>
            </select>
            <div class="new-subject-actions">
              <button class="btn btn-primary btn-sm" :disabled="savingSubject || !newSubjectName.trim()" @click="submitNewSubject">
                <i v-if="savingSubject" class="fas fa-spinner fa-spin"></i>
                <i v-else class="fas fa-check"></i>
                Guardar
              </button>
              <button class="btn btn-outline-secondary btn-sm" @click="showNewSubjectForm = false; subjectError = ''">
                Cancelar
              </button>
            </div>
            <p v-if="subjectError" class="subject-error"><i class="fas fa-exclamation-circle"></i> {{ subjectError }}</p>
          </div>

          <div v-if="curso.asignaturas?.length" class="subject-list">
            <div
              v-for="asig in curso.asignaturas"
              :key="asig.idAsignatura"
              class="subject-row-wrap"
            >
              <RouterLink
                :to="`/asignatura/${asig.idAsignatura}`"
                class="subject-row"
              >
                <div class="subject-icon"><i class="fas fa-book"></i></div>
                <div class="subject-info">
                  <div class="subject-name">{{ asig.nombre ?? asig.nombreAsignatura }}</div>
                  <div class="subject-meta" v-if="asig.numeroTemas != null">{{ asig.numeroTemas }} módulos &bull; {{ asig.numeroContenidos ?? 0 }} contenidos</div>
                </div>
                <i class="fas fa-arrow-right subject-arrow"></i>
              </RouterLink>
              <button
                v-if="auth.isAdmin"
                class="subject-delete-btn"
                title="Eliminar asignatura"
                @click.prevent="confirmDeleteSubject(asig.idAsignatura, asig.nombre ?? asig.nombreAsignatura ?? '')"
              >
                <i class="fas fa-trash"></i>
              </button>
            </div>
          </div>
          <div v-else-if="!showNewSubjectForm" class="empty-tab">
            <i class="fas fa-book"></i>
            <p>No hay asignaturas en este curso.</p>
          </div>
        </div>

        <!-- Tab: Alumnos -->
        <div v-else-if="activeTab === 'alumnos'" class="tab-panel">
          <div v-if="loadingEnrollments" class="loading-center"><div class="spinner"></div></div>
          <div v-else-if="errorEnrollments" class="error-tab">
            <i class="fas fa-exclamation-triangle"></i>
            <p>Error al cargar los alumnos.</p>
            <button class="btn btn-primary btn-sm" @click="loadEnrollments">Reintentar</button>
          </div>
          <div v-else-if="enrollments.length" class="enrollments-table-wrap">
            <table class="enrollments-table">
              <thead>
                <tr>
                  <th>Estudiante</th>
                  <th>Email</th>
                  <th>Inscripción</th>
                  <th>Progreso</th>
                  <th>Estado</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="e in enrollments" :key="e.idInscripcion">
                  <td class="student-cell">
                    <div class="student-av">{{ e.nombreEstudiante?.[0]?.toUpperCase() }}</div>
                    <span>{{ e.nombreEstudiante }}</span>
                  </td>
                  <td class="text-muted">{{ e.email }}</td>
                  <td class="text-muted">{{ e.fechaInscripcion ? new Date(e.fechaInscripcion).toLocaleDateString('es-ES') : '—' }}</td>
                  <td>
                    <div class="progress-wrap">
                      <div class="progress-track">
                        <div class="progress-fill" :style="{ width: `${e.porcentajeCompletado ?? 0}%` }"></div>
                      </div>
                      <span class="progress-label">{{ Math.round(e.porcentajeCompletado ?? 0) }}%</span>
                    </div>
                  </td>
                  <td>
                    <span class="badge" :class="(e.porcentajeCompletado ?? 0) >= 100 ? 'badge-activo' : 'badge-secondary'">
                      {{ (e.porcentajeCompletado ?? 0) >= 100 ? 'Completado' : 'En curso' }}
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
          <div v-else class="empty-tab">
            <i class="fas fa-users"></i>
            <p>No hay estudiantes inscritos en este curso.</p>
          </div>
        </div>

        <div v-else-if="activeTab === 'evaluacion'" class="tab-panel">
          <div class="empty-tab">
            <i class="fas fa-chart-bar"></i>
            <p>La evaluación por curso estará disponible en una próxima versión.</p>
          </div>
        </div>
        <div v-else class="tab-panel">
          <div class="empty-tab">
            <i class="fas fa-cog"></i>
            <p>La configuración avanzada del curso estará disponible en una próxima versión.</p>
          </div>
        </div>

      </template>
    </div>
  </div>
</template>

<style scoped>
.back-link {
  display: inline-flex;
  align-items: center;
  gap: var(--sp-2);
  color: var(--color-muted);
  font-size: var(--font-size-sm);
  margin-bottom: var(--sp-5);
  text-decoration: none;
  transition: color 0.15s;
}
.back-link:hover { color: var(--color-text); }

.header-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--sp-5) var(--sp-6);
  margin-bottom: var(--sp-4);
  box-shadow: var(--shadow-sm);
}
.header-main {
  display: flex;
  align-items: center;
  gap: var(--sp-3);
  margin-bottom: var(--sp-3);
}
.header-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  margin: 0;
}
.header-meta {
  display: flex;
  flex-wrap: wrap;
  gap: var(--sp-5);
  font-size: var(--font-size-sm);
  color: var(--color-muted);
}
.header-meta i { margin-right: var(--sp-1); }

.tabs-bar {
  display: flex;
  border-bottom: 1px solid var(--color-border);
  margin-bottom: var(--sp-6);
  gap: 0;
}

/* En móvil, si las tabs no caben, scroll horizontal interno con fade derecho
   en lugar de propagar el overflow a toda la página. */
@media (max-width: 768px) {
  .tabs-bar {
    overflow-x: auto;
    scrollbar-width: none;
    -ms-overflow-style: none;
    -webkit-mask-image: linear-gradient(to right, black 88%, transparent);
            mask-image: linear-gradient(to right, black 88%, transparent);
  }
  .tabs-bar::-webkit-scrollbar { display: none; }
}

.tab-btn {
  padding: var(--sp-3) var(--sp-5);
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  margin-bottom: -1px;
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-muted);
  cursor: pointer;
  white-space: nowrap;
  flex-shrink: 0;
  transition: color 0.15s, border-color 0.15s;
}
.tab-btn:hover { color: var(--color-text); }
.tab-btn.active {
  color: var(--color-primary);
  border-bottom-color: var(--color-primary);
  font-weight: var(--font-weight-semibold);
}

.tab-panel { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg); overflow: hidden; }

.subjects-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--sp-3) var(--sp-5);
  border-bottom: 1px solid var(--color-border);
  background: var(--color-muted-bg);
}
.subjects-count { font-size: var(--font-size-sm); color: var(--color-muted); }

.new-subject-form {
  padding: var(--sp-4) var(--sp-5);
  border-bottom: 1px solid var(--color-border);
  background: var(--color-surface);
  display: flex;
  flex-direction: column;
  gap: var(--sp-3);
}
.new-subject-actions { display: flex; gap: var(--sp-2); }
.subject-error {
  font-size: var(--font-size-sm);
  color: var(--color-danger);
  margin: 0;
  display: flex;
  align-items: center;
  gap: var(--sp-2);
}

.subject-list { display: flex; flex-direction: column; }
.subject-row-wrap {
  display: flex;
  align-items: stretch;
  border-bottom: 1px solid var(--color-border);
}
.subject-row-wrap:last-child { border-bottom: none; }
.subject-row {
  flex: 1;
  display: flex;
  align-items: center;
  gap: var(--sp-4);
  padding: var(--sp-4) var(--sp-5);
  text-decoration: none;
  color: var(--color-text);
  transition: background 0.15s;
}
.subject-row:hover { background: var(--color-muted-bg); }
.subject-delete-btn {
  flex-shrink: 0;
  background: none;
  border: none;
  border-left: 1px solid var(--color-border);
  padding: 0 var(--sp-4);
  color: var(--color-muted);
  cursor: pointer;
  font-size: 0.8rem;
  opacity: 0;
  transition: opacity 0.15s, background 0.15s, color 0.15s;
}
.subject-row-wrap:hover .subject-delete-btn { opacity: 1; }
.subject-delete-btn:hover { background: #fef2f2; color: var(--color-danger); }
.subject-icon {
  width: 32px; height: 32px;
  background: var(--color-muted-bg);
  border-radius: var(--radius-sm);
  display: flex; align-items: center; justify-content: center;
  color: var(--color-muted);
  flex-shrink: 0;
}
.subject-info { flex: 1; }
.subject-name { font-weight: var(--font-weight-medium); font-size: var(--font-size-sm); }
.subject-meta { font-size: var(--font-size-xs); color: var(--color-muted); margin-top: 2px; }
.subject-arrow { color: var(--color-muted); font-size: var(--font-size-sm); }

/* Alumnos tab */
.enrollments-table-wrap { overflow-x: auto; }
.enrollments-table {
  width: 100%;
  border-collapse: collapse;
  font-size: var(--font-size-sm);
}
.enrollments-table th {
  background: var(--color-muted-bg);
  padding: var(--sp-3) var(--sp-4);
  text-align: left;
  font-weight: var(--font-weight-semibold);
  border-bottom: 1px solid var(--color-border);
  color: var(--color-text);
}
.enrollments-table td {
  padding: var(--sp-3) var(--sp-4);
  border-bottom: 1px solid var(--color-border);
  vertical-align: middle;
}
.enrollments-table tr:last-child td { border-bottom: none; }
.enrollments-table tr:hover td { background: var(--color-muted-bg); }

.student-cell { display: flex; align-items: center; gap: var(--sp-3); }
.student-av {
  width: 32px; height: 32px; border-radius: 50%;
  background: var(--color-primary); color: white;
  display: flex; align-items: center; justify-content: center;
  font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold);
  flex-shrink: 0;
}
.text-muted { color: var(--color-muted); }

.progress-wrap { display: flex; align-items: center; gap: var(--sp-2); min-width: 120px; }
.progress-track { flex: 1; height: 8px; background: var(--color-border); border-radius: 999px; overflow: hidden; }
.progress-fill { height: 100%; background: var(--color-primary); border-radius: 999px; transition: width 0.3s; }
.progress-label { font-size: var(--font-size-xs); color: var(--color-muted); white-space: nowrap; }

.empty-tab {
  display: flex; flex-direction: column; align-items: center;
  gap: var(--sp-3); padding: var(--sp-10); text-align: center; color: var(--color-muted);
}
.empty-tab i { font-size: 2rem; opacity: 0.35; }
.empty-tab p { margin: 0; font-size: var(--font-size-sm); }

.error-tab {
  display: flex; flex-direction: column; align-items: center;
  gap: var(--sp-3); padding: var(--sp-8); text-align: center; color: var(--color-muted);
}
.error-tab i { font-size: 2rem; color: var(--color-danger); opacity: 0.7; }
.error-tab p { margin: 0; font-size: var(--font-size-sm); color: var(--color-text); }

.loading-center { display: flex; justify-content: center; padding: var(--sp-12) 0; }
.error-state {
  display: flex; flex-direction: column; align-items: center;
  gap: var(--sp-4); padding: var(--sp-12) 0; text-align: center;
}
.error-state i { font-size: 2.5rem; color: #dc2626; opacity: 0.7; }
.error-state p { margin: 0; color: var(--color-text); }
</style>
