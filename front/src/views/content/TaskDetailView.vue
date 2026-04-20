<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { getTask, submitTask, gradeTask } from '../../api/tasks'
import { formatDateTime } from '../../utils/format'
import type { TareaViewModel, EntregaTareaVM } from '../../types'
import CountdownTimer from '../../components/CountdownTimer.vue'
import FileUploader from '../../components/FileUploader.vue'

const route = useRoute()
const auth = useAuthStore()
const tarea = ref<TareaViewModel | null>(null)
const loading = ref(true)

const submitFile = ref<File | null>(null)
const submitComment = ref('')
const submitting = ref(false)

const gradeForm = ref({ idEntrega: 0, calificacion: 0, comentarios: '' })
const grading = ref(false)
const showGradeModal = ref(false)
const gradingStudent = ref('')

const deadline = computed(() => tarea.value?.fechaEntrega ?? '')
const isPastDeadline = computed(() => tarea.value ? new Date(tarea.value.fechaEntrega) < new Date() : false)
const canEdit = auth.isAdmin || ['Profesor', 'Tutor', 'Administrador'].includes(auth.role)
const canSubmit = !canEdit

async function load() {
  try {
    const res = await getTask(Number(route.params.id))
    tarea.value = res.data
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
    fd.append('comentarios', submitComment.value)
    await submitTask(Number(route.params.id), fd)
    await load()
  } finally {
    submitting.value = false
  }
}

function openGradeModal(entrega: EntregaTareaVM) {
  gradeForm.value = { idEntrega: entrega.idEntrega, calificacion: entrega.calificacion ?? 0, comentarios: entrega.comentarios ?? '' }
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
</script>

<template>
  <div class="container mt-4">
    <div v-if="loading" class="text-center py-5"><div class="spinner-border"></div></div>
    <template v-else-if="tarea">
      <div class="row">
        <div class="col-md-8 offset-md-2">
          <nav aria-label="breadcrumb">
            <ol class="breadcrumb">
              <li class="breadcrumb-item"><RouterLink to="/">Inicio</RouterLink></li>
              <li class="breadcrumb-item"><RouterLink :to="`/asignatura/${tarea.idAsignatura}`">{{ tarea.nombreAsignatura }}</RouterLink></li>
              <li class="breadcrumb-item active">{{ tarea.titulo }}</li>
            </ol>
          </nav>

          <div class="card shadow">
            <div class="card-header d-flex justify-content-between align-items-center bg-success text-white">
              <div class="d-flex align-items-center">
                <i class="fas fa-tasks me-3 fs-3"></i>
                <h4 class="mb-0">{{ tarea.titulo }}</h4>
              </div>
              <span class="badge bg-light text-success">Tarea</span>
            </div>

            <div class="card-body">
              <div class="row mb-4">
                <div class="col-md-6">
                  <div class="task-info">
                    <div class="info-item mb-2">
                      <i class="fas fa-calendar-alt text-success me-2"></i>
                      Entrega: <strong>{{ formatDateTime(tarea.fechaEntrega) }}</strong>
                    </div>
                    <div class="info-item mb-2">
                      <i class="fas fa-star text-success me-2"></i>
                      Puntuación máx.: <strong>{{ tarea.puntuacionMaxima }} pts</strong>
                    </div>
                    <div v-if="tarea.permiteEntregaTardia" class="info-item">
                      <i class="fas fa-exclamation-triangle text-success me-2"></i>
                      Entrega tardía: <strong>Permitida con penalización</strong>
                    </div>
                  </div>
                </div>
                <div class="col-md-6">
                  <div class="task-status p-3 rounded-3" :class="isPastDeadline ? 'bg-warning text-dark' : 'bg-light'">
                    <div class="d-flex align-items-center">
                      <i class="me-3" :class="isPastDeadline ? 'fas fa-exclamation-circle' : 'fas fa-hourglass-half text-success'"></i>
                      <div>
                        <h6 class="mb-0">{{ isPastDeadline ? 'Plazo finalizado' : 'Tiempo restante' }}</h6>
                        <small v-if="!isPastDeadline"><CountdownTimer :deadline="deadline" /></small>
                        <small v-else>La entrega podría tener penalización</small>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div class="mb-4">
                <h5>Descripción</h5>
                <p>{{ tarea.descripcion }}</p>
              </div>

              <div v-if="tarea.urlContenido" class="mb-4">
                <h5>Material de la tarea</h5>
                <div class="document-preview p-3 bg-light rounded mb-3 d-flex align-items-center">
                  <i class="fas fa-file-alt text-success fa-2x me-3"></i>
                  <div>
                    <h6 class="mb-1">{{ tarea.urlContenido.split('/').pop() }}</h6>
                    <small class="text-muted">Instrucciones de la tarea</small>
                  </div>
                </div>
                <div class="d-grid gap-2 col-md-6 mx-auto">
                  <a :href="`/uploads/contents/${tarea.urlContenido}`" download class="btn btn-outline-success">
                    <i class="fas fa-download me-2"></i>Descargar instrucciones
                  </a>
                </div>
              </div>

              <!-- Entrega del estudiante -->
              <div v-if="canSubmit" class="entrega-section mb-4">
                <h5>Tu entrega</h5>
                <div v-if="tarea.entrega" class="card border-success mb-3">
                  <div class="card-header bg-success bg-opacity-10">
                    <div class="d-flex justify-content-between align-items-center">
                      <strong>Entregado el: {{ formatDateTime(tarea.entrega.fechaEntrega) }}</strong>
                      <span class="badge" :class="new Date(tarea.entrega.fechaEntrega) > new Date(tarea.fechaEntrega) ? 'bg-warning text-dark' : 'bg-success'">
                        {{ new Date(tarea.entrega.fechaEntrega) > new Date(tarea.fechaEntrega) ? 'Tardía' : 'A tiempo' }}
                      </span>
                    </div>
                  </div>
                  <div class="card-body">
                    <div v-if="tarea.entrega.calificacion != null">
                      <div class="d-flex align-items-center mb-3">
                        <span class="me-3 fs-5">Calificación:</span>
                        <div class="progress flex-grow-1" style="height:25px">
                          <div class="progress-bar" :class="tarea.entrega.calificacion >= tarea.puntuacionAprobado ? 'bg-success' : 'bg-danger'"
                            :style="{ width: `${(tarea.entrega.calificacion / tarea.puntuacionMaxima) * 100}%` }">
                            {{ tarea.entrega.calificacion }} / {{ tarea.puntuacionMaxima }}
                          </div>
                        </div>
                      </div>
                      <h6>Comentarios:</h6>
                      <p class="p-3 bg-light rounded">{{ tarea.entrega.comentarios || 'Sin comentarios' }}</p>
                    </div>
                    <div v-else class="alert alert-info">
                      <i class="fas fa-info-circle me-2"></i>Tu entrega está pendiente de calificación.
                    </div>
                    <h6 class="mt-3">Archivo entregado:</h6>
                    <a :href="`/uploads/contents/${tarea.entrega.urlEntrega}`" download class="list-group-item list-group-item-action d-flex justify-content-between">
                      <span><i class="fas fa-file me-2"></i>{{ tarea.entrega.urlEntrega.split('/').pop() }}</span>
                      <span class="badge bg-secondary">Descargar</span>
                    </a>
                  </div>
                </div>
                <div v-else>
                  <div class="alert alert-warning"><i class="fas fa-exclamation-triangle me-2"></i>Aún no has realizado ninguna entrega.</div>
                  <form @submit.prevent="submit" class="mt-4">
                    <div class="mb-3">
                      <label class="form-label">Comentarios (opcional)</label>
                      <textarea v-model="submitComment" class="form-control" rows="3"></textarea>
                    </div>
                    <div class="mb-3">
                      <FileUploader label="Archivo para entregar" @change="f => submitFile = f" />
                    </div>
                    <button type="submit" class="btn btn-success w-100" :disabled="submitting || !submitFile">
                      <i class="fas fa-paper-plane me-2"></i>Realizar entrega
                    </button>
                  </form>
                </div>
              </div>

              <!-- Tabla de entregas para profesores -->
              <div v-if="canEdit" class="border-top pt-4">
                <h5 class="mb-3"><i class="fas fa-list-check text-success me-2"></i>Entregas de estudiantes</h5>
                <div v-if="tarea.entregas?.length" class="table-responsive">
                  <table class="table table-striped table-hover">
                    <thead>
                      <tr><th>Estudiante</th><th>Entregado</th><th>Estado</th><th>Calificación</th><th>Acciones</th></tr>
                    </thead>
                    <tbody>
                      <tr v-for="e in tarea.entregas" :key="e.idEntrega">
                        <td>{{ e.nombreEstudiante }}</td>
                        <td>
                          {{ formatDateTime(e.fechaEntrega) }}
                          <span v-if="new Date(e.fechaEntrega) > new Date(tarea.fechaEntrega)" class="badge bg-warning text-dark ms-2">Tardía</span>
                        </td>
                        <td>
                          <span class="badge" :class="e.calificacion != null ? 'bg-success' : 'bg-secondary'">
                            {{ e.calificacion != null ? 'Calificado' : 'Pendiente' }}
                          </span>
                        </td>
                        <td>
                          <span v-if="e.calificacion != null" :class="e.calificacion >= tarea.puntuacionAprobado ? 'text-success fw-bold' : 'text-danger fw-bold'">
                            {{ e.calificacion }} / {{ tarea.puntuacionMaxima }}
                          </span>
                          <span v-else>-</span>
                        </td>
                        <td>
                          <div class="btn-group">
                            <a :href="`/uploads/contents/${e.urlEntrega}`" download class="btn btn-sm btn-outline-primary"><i class="fas fa-download"></i></a>
                            <button class="btn btn-sm btn-outline-success" @click="openGradeModal(e)"><i class="fas fa-check-circle"></i></button>
                          </div>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                </div>
                <div v-else class="alert alert-info"><i class="fas fa-info-circle me-2"></i>No hay entregas para esta tarea.</div>
              </div>
            </div>

            <div class="card-footer bg-transparent">
              <div class="d-flex justify-content-between">
                <button class="btn btn-outline-secondary" @click="$router.back()">
                  <i class="fas fa-arrow-left me-2"></i>Volver
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Modal calificación -->
      <div v-if="showGradeModal" class="modal fade show d-block" tabindex="-1" style="background:rgba(0,0,0,0.5)">
        <div class="modal-dialog">
          <div class="modal-content">
            <div class="modal-header bg-success text-white">
              <h5 class="modal-title"><i class="fas fa-check-circle me-2"></i>Calificar Entrega</h5>
              <button type="button" class="btn-close" @click="showGradeModal = false"></button>
            </div>
            <div class="modal-body">
              <p><strong>Estudiante:</strong> {{ gradingStudent }}</p>
              <div class="mb-3">
                <label class="form-label">Calificación</label>
                <div class="input-group">
                  <input v-model.number="gradeForm.calificacion" type="number" class="form-control" :min="0" :max="tarea.puntuacionMaxima" step="0.1" required />
                  <span class="input-group-text">/ {{ tarea.puntuacionMaxima }}</span>
                </div>
              </div>
              <div class="mb-3">
                <label class="form-label">Comentarios</label>
                <textarea v-model="gradeForm.comentarios" class="form-control" rows="4"></textarea>
              </div>
            </div>
            <div class="modal-footer">
              <button class="btn btn-secondary" @click="showGradeModal = false">Cancelar</button>
              <button class="btn btn-success" @click="saveGrade" :disabled="grading">Guardar calificación</button>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
