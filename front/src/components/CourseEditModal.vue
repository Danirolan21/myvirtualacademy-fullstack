<script setup lang="ts">
import { ref, watch } from 'vue'
import { getCourse, updateCourse } from '../api/courses'
import FileUploader from './FileUploader.vue'

const props = defineProps<{ idCurso: number | null }>()
const emit = defineEmits<{ (e: 'saved'): void; (e: 'close'): void }>()

const ESTADOS = ['Borrador', 'Activo', 'Finalizado', 'Archivado', 'Suspendido']

const form = ref({
  nombre: '',
  descripcion: '',
  idProfesor: 0,
  idSuplente: '',
  fechaInicio: '',
  fechaFin: '',
  estado: 'Activo',
})
const profesores = ref<any[]>([])
const coverFile = ref<File | null>(null)
const loading = ref(false)
const saving = ref(false)

watch(() => props.idCurso, async (id) => {
  if (!id) return
  loading.value = true
  coverFile.value = null
  try {
    const res = await getCourse(id)
    const d = res.data
    const c = d.curso ?? d
    form.value = {
      nombre: c.nombreCurso ?? '',
      descripcion: c.descripcion ?? '',
      idProfesor: c.idProfesor ?? 0,
      idSuplente: c.idSuplente ?? '',
      fechaInicio: (c.fechaInicio ?? '').slice(0, 10),
      fechaFin: (c.fechaFin ?? '').slice(0, 10),
      estado: c.estado ?? 'Activo',
    }
    profesores.value = d.profesores ?? []
  } finally {
    loading.value = false
  }
})

async function save() {
  if (!props.idCurso) return
  saving.value = true
  try {
    const fd = new FormData()
    Object.entries(form.value).forEach(([k, v]) => fd.append(k, String(v)))
    if (coverFile.value) fd.append('imagenPortada', coverFile.value)
    await updateCourse(props.idCurso, fd)
    emit('saved')
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <Teleport to="body">
    <div v-if="idCurso" class="modal-overlay" @mousedown.self="emit('close')">
      <div class="modal-box" role="dialog" aria-modal="true" aria-labelledby="modal-title">

        <!-- Header -->
        <div class="modal-head">
          <h5 id="modal-title" class="modal-title">Editar Curso</h5>
          <button class="modal-close-btn" type="button" @click="emit('close')" aria-label="Cerrar">
            <i class="fas fa-times"></i>
          </button>
        </div>

        <!-- Body -->
        <div class="modal-body">
          <div v-if="loading" class="loading-center">
            <div class="spinner"></div>
          </div>
          <form v-else @submit.prevent="save" id="editCourseForm" novalidate>

            <div class="field">
              <label class="form-label" for="ec-nombre">Nombre</label>
              <input id="ec-nombre" v-model="form.nombre" type="text" class="form-control" required />
            </div>

            <div class="field">
              <label class="form-label" for="ec-desc">Descripción</label>
              <textarea id="ec-desc" v-model="form.descripcion" class="form-control" rows="3"></textarea>
            </div>

            <div class="field">
              <label class="form-label" for="ec-prof">Profesor</label>
              <select id="ec-prof" v-model.number="form.idProfesor" class="form-select" required>
                <option v-for="p in profesores" :key="p.idUsuario" :value="p.idUsuario">
                  {{ p.nombre }} {{ p.apellidos }}
                </option>
              </select>
            </div>

            <div class="field">
              <label class="form-label" for="ec-suplente">Profesor Suplente (Opcional)</label>
              <select id="ec-suplente" v-model="form.idSuplente" class="form-select">
                <option value="">-- Sin suplente --</option>
                <option v-for="p in profesores" :key="p.idUsuario" :value="p.idUsuario">
                  {{ p.nombre }} {{ p.apellidos }}
                </option>
              </select>
            </div>

            <div class="field-row">
              <div class="field">
                <label class="form-label" for="ec-inicio">Fecha Inicio</label>
                <input id="ec-inicio" v-model="form.fechaInicio" type="date" class="form-control" required />
              </div>
              <div class="field">
                <label class="form-label" for="ec-fin">Fecha Fin</label>
                <input id="ec-fin" v-model="form.fechaFin" type="date" class="form-control" required />
              </div>
            </div>

            <div class="field">
              <label class="form-label" for="ec-estado">Estado</label>
              <select id="ec-estado" v-model="form.estado" class="form-select" required>
                <option v-for="e in ESTADOS" :key="e">{{ e }}</option>
              </select>
            </div>

            <div class="field">
              <FileUploader label="Imagen Portada (opcional)" accept="image/*" @change="f => coverFile = f" />
            </div>

          </form>
        </div>

        <!-- Footer -->
        <div class="modal-foot">
          <button type="button" class="btn btn-secondary" @click="emit('close')">Cancelar</button>
          <button type="submit" form="editCourseForm" class="btn btn-primary" :disabled="saving">
            <span v-if="saving"><i class="fas fa-spinner fa-spin me-2"></i>Guardando…</span>
            <span v-else>Guardar</span>
          </button>
        </div>

      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1050;
  padding: var(--sp-4);
}

.modal-box {
  background: var(--color-surface);
  border-radius: var(--radius-lg);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.25);
  width: 100%;
  max-width: 550px;
  max-height: 90vh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

/* Header */
.modal-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--sp-4) var(--sp-6);
  border-bottom: 1px solid var(--color-border);
  flex-shrink: 0;
}

.modal-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0;
}

.modal-close-btn {
  background: none;
  border: none;
  cursor: pointer;
  color: var(--color-muted);
  font-size: 1rem;
  padding: var(--sp-1) var(--sp-2);
  border-radius: var(--radius-sm);
  line-height: 1;
  transition: color 0.15s, background 0.15s;
}
.modal-close-btn:hover {
  color: var(--color-text);
  background: var(--color-muted-bg);
}

/* Body */
.modal-body {
  padding: var(--sp-6);
  overflow-y: auto;
  flex: 1;
}

.field {
  margin-bottom: var(--sp-4);
}

.field-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: var(--sp-4);
  margin-bottom: var(--sp-4);
}

.loading-center {
  display: flex;
  justify-content: center;
  padding: var(--sp-8) 0;
}

/* Footer */
.modal-foot {
  display: flex;
  justify-content: flex-end;
  gap: var(--sp-3);
  padding: var(--sp-4) var(--sp-6);
  border-top: 1px solid var(--color-border);
  flex-shrink: 0;
  background: var(--color-surface);
}

/* btn-secondary local override (muted gray, not outline) */
.btn.btn-secondary {
  background: var(--color-muted);
  border-color: var(--color-muted);
  color: #fff;
}
.btn.btn-secondary:hover:not(:disabled) {
  background: #5a6268;
  border-color: #5a6268;
}
</style>
