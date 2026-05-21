<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { getTask, submitTask, gradeTask } from '../../api/tasks'
import { updateContent, registerView } from '../../api/content'
import { formatDateTime } from '../../utils/format'
import { contentFileUrl } from '../../utils/images'
import type { TareaViewModel, EntregaTareaVM } from '../../types'
import CountdownTimer from '../../components/CountdownTimer.vue'
import FileUploader from '../../components/FileUploader.vue'

const route = useRoute()
const auth = useAuthStore()
const tarea = ref<TareaViewModel | null>(null)
const loading = ref(true)
const error = ref(false)

const submitFile = ref<File | null>(null)
const submitComment = ref('')
const submitting = ref(false)

const gradeForm = ref({ idEntrega: 0, idEstudiante: 0, calificacion: 0, comentarios: '' })
const grading = ref(false)
const showGradeModal = ref(false)
const gradingStudent = ref('')

const showEditModal = ref(false)
const editForm = ref({ titulo: '', descripcion: '', fechaEntrega: '', puntuacionMaxima: 10, permiteEntregaTardia: false })
const editFile = ref<File | null>(null)
const saving = ref(false)

const deadline = computed(() => tarea.value?.fechaEntrega ?? '')
const isPastDeadline = computed(() => tarea.value ? new Date(tarea.value.fechaEntrega) < new Date() : false)
const canEdit = computed(() => auth.isAdmin || ['Profesor', 'Tutor', 'Administrador'].includes(auth.role))
const canSubmit = computed(() => !canEdit.value)

async function load() {
  loading.value = true
  error.value = false
  try {
    const res = await getTask(Number(route.params.id))
    tarea.value = res.data
  } catch {
    error.value = true
  } finally {
    loading.value = false
  }
}

onMounted(load)

async function submit() {
  if (!submitFile.value) return
  submitting.value = true
  try {
    const fd = new FormData()
    fd.append('archivoEntrega', submitFile.value)
    fd.append('comentarioEstudiante', submitComment.value)
    await submitTask(Number(route.params.id), fd)
    await registerView(Number(route.params.id))
    await load()
  } finally {
    submitting.value = false
  }
}

function openGradeModal(entrega: EntregaTareaVM) {
  gradeForm.value = {
    idEntrega: entrega.idEntrega,
    idEstudiante: entrega.idEstudiante ?? 0,
    calificacion: entrega.calificacion ?? 0,
    comentarios: entrega.comentarios ?? '',
  }
  gradingStudent.value = entrega.nombreEstudiante ?? ''
  showGradeModal.value = true
}

async function saveGrade() {
  grading.value = true
  try {
    await gradeTask(Number(route.params.id), gradeForm.value)
    showGradeModal.value = false
    await load()
  } finally {
    grading.value = false
  }
}

function openEditModal() {
  if (!tarea.value) return
  const dt = tarea.value.fechaEntrega
    ? new Date(tarea.value.fechaEntrega).toISOString().slice(0, 16)
    : ''
  editForm.value = {
    titulo: tarea.value.titulo,
    descripcion: tarea.value.descripcion ?? '',
    fechaEntrega: dt,
    puntuacionMaxima: tarea.value.puntuacionMaxima,
    permiteEntregaTardia: tarea.value.permiteEntregaTardia,
  }
  editFile.value = null
  showEditModal.value = true
}

async function saveEdit() {
  saving.value = true
  try {
    const fd = new FormData()
    fd.append('titulo', editForm.value.titulo)
    fd.append('descripcion', editForm.value.descripcion)
    fd.append('fechaEntrega', editForm.value.fechaEntrega)
    fd.append('puntuacionMaxima', String(editForm.value.puntuacionMaxima))
    fd.append('permiteEntregaTardia', String(editForm.value.permiteEntregaTardia))
    if (editFile.value) fd.append('archivoContenido', editFile.value)
    await updateContent(Number(route.params.id), fd)
    showEditModal.value = false
    await load()
  } finally {
    saving.value = false
  }
}

function isLate(fechaEntregada: string) {
  return tarea.value ? new Date(fechaEntregada) > new Date(tarea.value.fechaEntrega) : false
}
</script>

<template>
  <div class="page">
    <div class="task-container">
      <div v-if="loading" class="loading-center"><div class="spinner"></div></div>

      <div v-else-if="error" class="error-state">
        <i class="fas fa-exclamation-triangle"></i>
        <p>Error al cargar los datos de la tarea. Inténtalo de nuevo.</p>
        <button class="btn btn-primary" @click="load">Reintentar</button>
      </div>

      <template v-else-if="tarea">
        <!-- Breadcrumb -->
        <nav class="breadcrumb">
          <RouterLink to="/" class="breadcrumb-item">Inicio</RouterLink>
          <span class="breadcrumb-sep"><i class="fas fa-chevron-right"></i></span>
          <RouterLink :to="`/asignatura/${tarea.idAsignatura}`" class="breadcrumb-item">{{ tarea.nombreAsignatura }}</RouterLink>
          <span class="breadcrumb-sep"><i class="fas fa-chevron-right"></i></span>
          <span class="breadcrumb-item breadcrumb-active">{{ tarea.titulo }}</span>
        </nav>

        <!-- Header card -->
        <div class="task-card">
          <div class="task-header">
            <div class="task-header-left">
              <div class="task-header-icon"><i class="fas fa-tasks"></i></div>
              <div>
                <h1 class="task-title">{{ tarea.titulo }}</h1>
                <span class="task-type-badge">Tarea</span>
              </div>
            </div>
            <button v-if="canEdit" class="btn btn-sm btn-outline-light" @click="openEditModal">
              <i class="fas fa-edit"></i> Editar
            </button>
          </div>

          <div class="task-body">
            <!-- Info grid -->
            <div class="info-grid">
              <div class="info-panel">
                <div class="info-row">
                  <i class="fas fa-calendar-alt info-icon"></i>
                  <div>
                    <div class="info-label">Fecha de entrega</div>
                    <div class="info-val">{{ formatDateTime(tarea.fechaEntrega) }}</div>
                  </div>
                </div>
                <div class="info-row">
                  <i class="fas fa-star info-icon"></i>
                  <div>
                    <div class="info-label">Puntuación máxima</div>
                    <div class="info-val">{{ tarea.puntuacionMaxima }} pts</div>
                  </div>
                </div>
                <div v-if="tarea.permiteEntregaTardia" class="info-row">
                  <i class="fas fa-exclamation-triangle info-icon"></i>
                  <div>
                    <div class="info-label">Entrega tardía</div>
                    <div class="info-val">Permitida con penalización</div>
                  </div>
                </div>
              </div>

              <div class="countdown-panel" :class="isPastDeadline ? 'countdown-expired' : 'countdown-active'">
                <div class="countdown-inner">
                  <i class="fas" :class="isPastDeadline ? 'fa-exclamation-circle' : 'fa-hourglass-half'"></i>
                  <div>
                    <div class="countdown-label">{{ isPastDeadline ? 'Plazo finalizado' : 'Tiempo restante' }}</div>
                    <div class="countdown-val" v-if="!isPastDeadline">
                      <CountdownTimer :deadline="deadline" />
                    </div>
                    <div class="countdown-note" v-else>La entrega podría tener penalización</div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Description -->
            <div class="section-block">
              <h5 class="section-title">Descripción</h5>
              <p class="section-text">{{ tarea.descripcion }}</p>
            </div>

            <!-- Material -->
            <div v-if="tarea.urlContenido" class="section-block">
              <h5 class="section-title">Material de la tarea</h5>
              <div class="material-row">
                <div class="material-icon"><i class="fas fa-file-alt"></i></div>
                <div class="material-info">
                  <div class="material-name">{{ tarea.urlContenido.split('/').pop() }}</div>
                  <div class="material-sub">Instrucciones de la tarea</div>
                </div>
                <a :href="contentFileUrl(tarea.urlContenido)" download class="btn btn-sm btn-outline-secondary">
                  <i class="fas fa-download"></i> Descargar
                </a>
              </div>
            </div>

            <!-- Student submission -->
            <div v-if="canSubmit" class="section-block">
              <h5 class="section-title">Tu entrega</h5>

              <div v-if="tarea.entrega" class="submission-card">
                <div class="submission-header">
                  <span class="submission-date">
                    <i class="fas fa-clock"></i> Entregado el {{ formatDateTime(tarea.entrega.fechaEntrega) }}
                  </span>
                  <span class="badge" :class="isLate(tarea.entrega.fechaEntrega) ? 'badge-borrador' : 'badge-activo'">
                    {{ isLate(tarea.entrega.fechaEntrega) ? 'Tardía' : 'A tiempo' }}
                  </span>
                </div>
                <div class="submission-body">
                  <div v-if="tarea.entrega.calificacion != null">
                    <div class="grade-row">
                      <span class="grade-label">Calificación</span>
                      <div class="grade-bar-wrap">
                        <div class="grade-track">
                          <div
                            class="grade-fill"
                            :class="tarea.entrega.calificacion >= tarea.puntuacionAprobado ? 'fill-pass' : 'fill-fail'"
                            :style="{ width: `${(tarea.entrega.calificacion / tarea.puntuacionMaxima) * 100}%` }"
                          ></div>
                        </div>
                        <span class="grade-text" :class="tarea.entrega.calificacion >= tarea.puntuacionAprobado ? 'text-pass' : 'text-fail'">
                          {{ tarea.entrega.calificacion }} / {{ tarea.puntuacionMaxima }}
                        </span>
                      </div>
                    </div>
                    <div class="comment-block">
                      <div class="comment-label"><i class="fas fa-comment-alt"></i> Comentarios del profesor</div>
                      <p class="comment-text">{{ tarea.entrega.comentarios || 'Sin comentarios' }}</p>
                    </div>
                  </div>
                  <div v-else class="alert alert-info">
                    <i class="fas fa-info-circle"></i> Tu entrega está pendiente de calificación.
                  </div>
                  <div class="file-row">
                    <i class="fas fa-file file-icon"></i>
                    <span>{{ tarea.entrega.urlEntrega.split('/').pop() }}</span>
                    <a :href="contentFileUrl(tarea.entrega.urlEntrega)" download class="btn btn-sm btn-outline-secondary ms-auto">
                      <i class="fas fa-download"></i>
                    </a>
                  </div>
                </div>
              </div>

              <div v-else>
                <div class="alert alert-warning">
                  <i class="fas fa-exclamation-triangle"></i> Aún no has realizado ninguna entrega.
                </div>
                <form @submit.prevent="submit" class="submit-form">
                  <div class="field">
                    <label class="form-label">Comentarios (opcional)</label>
                    <textarea v-model="submitComment" class="form-control" rows="3"></textarea>
                  </div>
                  <div class="field">
                    <FileUploader label="Archivo para entregar" @change="f => submitFile = f" />
                  </div>
                  <button type="submit" class="btn btn-success btn-block" :disabled="submitting || !submitFile">
                    <span v-if="submitting"><i class="fas fa-spinner fa-spin"></i> Enviando…</span>
                    <span v-else><i class="fas fa-paper-plane"></i> Realizar entrega</span>
                  </button>
                </form>
              </div>
            </div>

            <!-- Professor: submissions table -->
            <div v-if="canEdit" class="section-block">
              <h5 class="section-title">
                <i class="fas fa-list-check"></i> Entregas de estudiantes
              </h5>

              <div v-if="tarea.entregas?.length">
                <!-- Desktop table -->
                <div class="submissions-table-wrap">
                  <table class="submissions-table">
                    <thead>
                      <tr>
                        <th>Estudiante</th>
                        <th>Entregado</th>
                        <th>Estado</th>
                        <th>Calificación</th>
                        <th>Acciones</th>
                      </tr>
                    </thead>
                    <tbody>
                      <tr v-for="e in tarea.entregas" :key="e.idEntrega">
                        <td>{{ e.nombreEstudiante }}</td>
                        <td>
                          {{ formatDateTime(e.fechaEntrega) }}
                          <span v-if="isLate(e.fechaEntrega)" class="badge badge-borrador ms-1">Tardía</span>
                        </td>
                        <td>
                          <span class="badge" :class="e.calificacion != null ? 'badge-activo' : 'badge-secondary'">
                            {{ e.calificacion != null ? 'Calificado' : 'Pendiente' }}
                          </span>
                        </td>
                        <td>
                          <span v-if="e.calificacion != null" :class="e.calificacion >= tarea.puntuacionAprobado ? 'text-pass fw-bold' : 'text-fail fw-bold'">
                            {{ e.calificacion }} / {{ tarea.puntuacionMaxima }}
                          </span>
                          <span v-else class="text-muted">—</span>
                        </td>
                        <td>
                          <div class="action-btns">
                            <a :href="contentFileUrl(e.urlEntrega)" download class="btn btn-sm btn-outline-secondary">
                              <i class="fas fa-download"></i>
                            </a>
                            <button class="btn btn-sm btn-success" @click="openGradeModal(e)">
                              <i class="fas fa-check-circle"></i> Calificar
                            </button>
                          </div>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>

                <!-- Mobile cards -->
                <div class="submission-cards">
                  <div class="submission-mobile-card" v-for="e in tarea.entregas" :key="e.idEntrega">
                    <div class="smc-header">
                      <strong>{{ e.nombreEstudiante }}</strong>
                      <span class="badge" :class="e.calificacion != null ? 'badge-activo' : 'badge-secondary'">
                        {{ e.calificacion != null ? 'Calificado' : 'Pendiente' }}
                      </span>
                    </div>
                    <div class="smc-row">
                      <span class="smc-label">Entregado</span>
                      <span>{{ formatDateTime(e.fechaEntrega) }}
                        <span v-if="isLate(e.fechaEntrega)" class="badge badge-borrador ms-1">Tardía</span>
                      </span>
                    </div>
                    <div class="smc-row" v-if="e.calificacion != null">
                      <span class="smc-label">Calificación</span>
                      <span :class="e.calificacion >= tarea.puntuacionAprobado ? 'text-pass' : 'text-fail'">
                        {{ e.calificacion }} / {{ tarea.puntuacionMaxima }}
                      </span>
                    </div>
                    <div class="smc-actions">
                      <a :href="contentFileUrl(e.urlEntrega)" download class="btn btn-sm btn-outline-secondary">
                        <i class="fas fa-download"></i> Archivo
                      </a>
                      <button class="btn btn-sm btn-success" @click="openGradeModal(e)">
                        <i class="fas fa-check-circle"></i> Calificar
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <div v-else class="alert alert-info">
                <i class="fas fa-info-circle"></i> No hay entregas para esta tarea.
              </div>
            </div>

          </div>

          <div class="task-footer">
            <button class="btn btn-outline-secondary" @click="$router.back()">
              <i class="fas fa-arrow-left"></i> Volver
            </button>
          </div>
        </div>
      </template>
    </div>
  </div>

  <!-- Grade modal -->
  <Teleport to="body">
    <div v-if="showGradeModal && tarea" class="modal-overlay" @mousedown.self="showGradeModal = false">
      <div class="modal-dialog">
        <div class="modal-header modal-header-success">
          <h5 class="modal-title"><i class="fas fa-check-circle"></i> Calificar Entrega</h5>
          <button class="modal-close" @click="showGradeModal = false"><i class="fas fa-times"></i></button>
        </div>
        <div class="modal-body">
          <p class="student-name"><strong>Estudiante:</strong> {{ gradingStudent }}</p>
          <div class="field">
            <label class="form-label">Calificación</label>
            <div class="input-score-wrap">
              <input v-model.number="gradeForm.calificacion" type="number" class="form-control" :min="0" :max="tarea.puntuacionMaxima" step="0.1" required />
              <span class="input-score-suffix">/ {{ tarea.puntuacionMaxima }}</span>
            </div>
          </div>
          <div class="field">
            <label class="form-label">Comentarios</label>
            <textarea v-model="gradeForm.comentarios" class="form-control" rows="4"></textarea>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-outline-secondary" @click="showGradeModal = false">Cancelar</button>
          <button class="btn btn-success" @click="saveGrade" :disabled="grading">
            <span v-if="grading"><i class="fas fa-spinner fa-spin"></i> Guardando…</span>
            <span v-else>Guardar calificación</span>
          </button>
        </div>
      </div>
    </div>
  </Teleport>

  <!-- Edit task modal -->
  <Teleport to="body">
    <div v-if="showEditModal" class="modal-overlay" @mousedown.self="showEditModal = false">
      <div class="modal-dialog modal-dialog-lg">
        <div class="modal-header">
          <h5 class="modal-title"><i class="fas fa-edit"></i> Editar Tarea</h5>
          <button class="modal-close" @click="showEditModal = false"><i class="fas fa-times"></i></button>
        </div>
        <div class="modal-body">
          <div class="field">
            <label class="form-label">Título <span class="req">*</span></label>
            <input v-model="editForm.titulo" type="text" class="form-control" required />
          </div>
          <div class="field">
            <label class="form-label">Descripción</label>
            <textarea v-model="editForm.descripcion" class="form-control" rows="3"></textarea>
          </div>
          <div class="field-row">
            <div class="field">
              <label class="form-label">Fecha de entrega <span class="req">*</span></label>
              <input v-model="editForm.fechaEntrega" type="datetime-local" class="form-control" required />
            </div>
            <div class="field">
              <label class="form-label">Puntuación máxima</label>
              <input v-model.number="editForm.puntuacionMaxima" type="number" class="form-control" min="0" step="0.5" />
            </div>
          </div>
          <div class="field checkbox-field">
            <label class="checkbox-label">
              <input v-model="editForm.permiteEntregaTardia" type="checkbox" class="checkbox-input" />
              <span>Permitir entregas tardías</span>
            </label>
          </div>
          <div class="field">
            <label class="form-label">Instrucciones (archivo)</label>
            <FileUploader label="Reemplazar archivo de instrucciones" @change="f => editFile = f" />
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-outline-secondary" @click="showEditModal = false">Cancelar</button>
          <button class="btn btn-primary" @click="saveEdit" :disabled="saving">
            <span v-if="saving"><i class="fas fa-spinner fa-spin"></i> Guardando…</span>
            <span v-else><i class="fas fa-save"></i> Guardar</span>
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.task-container {
  max-width: 800px;
  margin: 0 auto;
  padding: var(--sp-6) var(--sp-4);
}

/* Breadcrumb */
.breadcrumb {
  display: flex;
  align-items: center;
  gap: var(--sp-2);
  flex-wrap: wrap;
  margin-bottom: var(--sp-4);
  font-size: var(--font-size-sm);
}
.breadcrumb-item { color: var(--color-muted); text-decoration: none; transition: color 0.15s; }
.breadcrumb-item:hover { color: var(--color-text); }
.breadcrumb-active { color: var(--color-text); font-weight: var(--font-weight-medium); }
.breadcrumb-sep { color: var(--color-muted); font-size: 10px; }

/* Task card */
.task-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  overflow: hidden;
  box-shadow: var(--shadow-sm);
}

.task-header {
  background: #198754;
  padding: var(--sp-5) var(--sp-6);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--sp-4);
}
.task-header-left { display: flex; align-items: center; gap: var(--sp-4); }
.task-header-icon {
  width: 44px; height: 44px;
  background: rgba(255,255,255,0.2);
  border-radius: var(--radius-md);
  display: flex; align-items: center; justify-content: center;
  color: white;
  font-size: var(--font-size-xl);
  flex-shrink: 0;
}
.task-title {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  color: white;
  margin: 0 0 var(--sp-1);
}
.task-type-badge {
  display: inline-block;
  background: rgba(255,255,255,0.25);
  color: white;
  border-radius: 999px;
  padding: 2px 10px;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
}
.btn-outline-light {
  background: rgba(255,255,255,0.1);
  border: 1px solid rgba(255,255,255,0.4);
  color: white;
  padding: var(--sp-1) var(--sp-3);
  border-radius: var(--radius-md);
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  cursor: pointer;
  display: inline-flex; align-items: center; gap: var(--sp-1);
  transition: background 0.15s;
}
.btn-outline-light:hover { background: rgba(255,255,255,0.2); }

.task-body { padding: var(--sp-6); display: flex; flex-direction: column; gap: var(--sp-6); }
.task-footer {
  padding: var(--sp-4) var(--sp-6);
  border-top: 1px solid var(--color-border);
  background: var(--color-muted-bg);
}

/* Info grid */
.info-grid { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-4); }
.info-panel { display: flex; flex-direction: column; gap: var(--sp-3); }
.info-row { display: flex; align-items: flex-start; gap: var(--sp-3); }
.info-icon { color: #198754; margin-top: 2px; width: 16px; text-align: center; }
.info-label { font-size: var(--font-size-xs); color: var(--color-muted); }
.info-val { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text); }

.countdown-panel {
  border-radius: var(--radius-md);
  padding: var(--sp-4);
  display: flex; align-items: center;
}
.countdown-active { background: #d1fae5; }
.countdown-expired { background: #fef3c7; }
.countdown-inner { display: flex; align-items: flex-start; gap: var(--sp-3); font-size: var(--font-size-xl); }
.countdown-label { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); color: var(--color-text); }
.countdown-val { font-size: var(--font-size-md); font-weight: var(--font-weight-bold); color: #065f46; }
.countdown-note { font-size: var(--font-size-sm); color: #92400e; }

/* Section blocks */
.section-block {}
.section-title {
  font-size: var(--font-size-md);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0 0 var(--sp-3);
  display: flex; align-items: center; gap: var(--sp-2);
}
.section-text { font-size: var(--font-size-sm); color: var(--color-muted); margin: 0; }

/* Material row */
.material-row {
  display: flex;
  align-items: center;
  gap: var(--sp-3);
  background: var(--color-muted-bg);
  border-radius: var(--radius-md);
  padding: var(--sp-3) var(--sp-4);
  border: 1px solid var(--color-border);
}
.material-icon { color: #198754; font-size: var(--font-size-xl); flex-shrink: 0; }
.material-info { flex: 1; min-width: 0; }
.material-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.material-sub { font-size: var(--font-size-xs); color: var(--color-muted); }

/* En móvil el nombre del archivo es muy largo y empuja el botón fuera.
   Permitir que el botón "Descargar" baje a una segunda línea ocupando todo
   el ancho — más usable que un botón cortado fuera del viewport. */
@media (max-width: 768px) {
  .material-row { flex-wrap: wrap; }
  .material-row > .btn { width: 100%; justify-content: center; }
}

/* Submission card */
.submission-card {
  border: 1px solid #a7f3d0;
  border-radius: var(--radius-md);
  overflow: hidden;
}
.submission-header {
  background: #d1fae5;
  padding: var(--sp-3) var(--sp-4);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--sp-3);
}
.submission-date { font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); color: #065f46; display: flex; align-items: center; gap: var(--sp-2); }
.submission-body { padding: var(--sp-4); display: flex; flex-direction: column; gap: var(--sp-4); }

.grade-row { display: flex; align-items: center; gap: var(--sp-3); }
.grade-label { font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); white-space: nowrap; }
.grade-bar-wrap { flex: 1; display: flex; align-items: center; gap: var(--sp-3); }
.grade-track { flex: 1; height: 20px; background: #e5e7eb; border-radius: 999px; overflow: hidden; }
.grade-fill { height: 100%; border-radius: 999px; }
.fill-pass { background: #198754; }
.fill-fail { background: var(--color-danger); }
.grade-text { font-size: var(--font-size-sm); font-weight: var(--font-weight-bold); white-space: nowrap; }
.text-pass { color: #198754; }
.text-fail { color: var(--color-danger); }

.comment-block {}
.comment-label { font-size: var(--font-size-sm); font-weight: var(--font-weight-semibold); margin-bottom: var(--sp-2); display: flex; align-items: center; gap: var(--sp-2); }
.comment-text { background: var(--color-muted-bg); border-radius: var(--radius-md); padding: var(--sp-3); font-size: var(--font-size-sm); color: var(--color-muted); margin: 0; }

.file-row {
  display: flex;
  align-items: center;
  gap: var(--sp-3);
  background: var(--color-muted-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: var(--sp-2) var(--sp-3);
  font-size: var(--font-size-sm);
}
.file-row > span {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.file-icon { color: var(--color-muted); flex-shrink: 0; }
.file-row > .btn { flex-shrink: 0; }
.ms-auto { margin-left: auto; }

/* Submit form */
.submit-form { display: flex; flex-direction: column; gap: var(--sp-4); }
.field { display: flex; flex-direction: column; gap: var(--sp-1); }
.btn-block { width: 100%; justify-content: center; }

/* Submissions table */
.submissions-table-wrap {
  overflow-x: auto;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
}
.submissions-table {
  width: 100%;
  border-collapse: collapse;
  font-size: var(--font-size-sm);
}
.submissions-table th {
  background: var(--color-muted-bg);
  padding: var(--sp-3) var(--sp-4);
  text-align: left;
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  border-bottom: 1px solid var(--color-border);
}
.submissions-table td {
  padding: var(--sp-3) var(--sp-4);
  border-bottom: 1px solid var(--color-border);
  color: var(--color-text);
  vertical-align: middle;
}
.submissions-table tr:last-child td { border-bottom: none; }
.submissions-table tr:hover td { background: var(--color-muted-bg); }
.action-btns { display: flex; gap: var(--sp-2); }
.fw-bold { font-weight: var(--font-weight-bold); }
.text-muted { color: var(--color-muted); }
.ms-1 { margin-left: var(--sp-1); }

/* Mobile cards for submissions */
.submission-cards { display: none; flex-direction: column; gap: var(--sp-3); }
.submission-mobile-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: var(--sp-4);
  display: flex;
  flex-direction: column;
  gap: var(--sp-2);
}
.smc-header { display: flex; justify-content: space-between; align-items: center; gap: var(--sp-2); }
.smc-row { display: flex; justify-content: space-between; align-items: center; font-size: var(--font-size-sm); }
.smc-label { color: var(--color-muted); }
.smc-actions { display: flex; gap: var(--sp-2); padding-top: var(--sp-2); }

/* Alerts */
.alert { display: flex; align-items: center; gap: var(--sp-2); padding: var(--sp-3) var(--sp-4); border-radius: var(--radius-md); font-size: var(--font-size-sm); }
.alert-info { background: #dbeafe; color: #1e40af; }
.alert-warning { background: #fef3c7; color: #92400e; }

/* Modal */
.modal-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: var(--sp-5) var(--sp-6);
  border-bottom: 1px solid var(--color-border);
}
.modal-header-success { background: #198754; }
.modal-header-success .modal-title { color: white; }
.modal-header-success .modal-close { color: rgba(255,255,255,0.7); }
.modal-header-success .modal-close:hover { color: white; }
.modal-title { font-size: var(--font-size-lg); font-weight: var(--font-weight-semibold); margin: 0; display: flex; align-items: center; gap: var(--sp-2); color: var(--color-text); }
.modal-close { background: none; border: none; font-size: var(--font-size-md); cursor: pointer; padding: var(--sp-1); transition: color 0.15s; }
.modal-body { padding: var(--sp-5) var(--sp-6); display: flex; flex-direction: column; gap: var(--sp-4); }
.modal-footer { display: flex; justify-content: flex-end; gap: var(--sp-3); padding: var(--sp-4) var(--sp-6); border-top: 1px solid var(--color-border); }
.modal-dialog-lg { max-width: 600px; }

.student-name { font-size: var(--font-size-sm); margin: 0; }
.input-score-wrap { display: flex; align-items: center; gap: var(--sp-2); }
.input-score-suffix { font-size: var(--font-size-sm); color: var(--color-muted); white-space: nowrap; }

.field-row { display: grid; grid-template-columns: 1fr 1fr; gap: var(--sp-4); }
.req { color: var(--color-danger); }

.checkbox-field { flex-direction: row; align-items: center; }
.checkbox-label { display: flex; align-items: center; gap: var(--sp-2); font-size: var(--font-size-sm); cursor: pointer; }
.checkbox-input { width: 16px; height: 16px; cursor: pointer; }

.loading-center { display: flex; justify-content: center; padding: var(--sp-12) 0; }
.error-state {
  display: flex; flex-direction: column; align-items: center;
  gap: var(--sp-4); padding: var(--sp-12) 0; text-align: center;
}
.error-state i { font-size: 2.5rem; color: #dc2626; opacity: 0.7; }
.error-state p { margin: 0; color: var(--color-text); }

@media (max-width: 768px) {
  .info-grid { grid-template-columns: 1fr; }
  .submissions-table-wrap { display: none; }
  .submission-cards { display: flex; }
  .field-row { grid-template-columns: 1fr; }
}
</style>
