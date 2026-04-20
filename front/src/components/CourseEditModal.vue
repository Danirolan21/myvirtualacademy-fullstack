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
  try {
    const res = await getCourse(id)
    const d = res.data
    // GetById devuelve { curso: {...}, profesores: [...], ... }
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
  <div v-if="idCurso" class="modal fade show d-block" tabindex="-1" style="background:rgba(0,0,0,0.5)">
    <div class="modal-dialog modal-lg">
      <div class="modal-content">
        <div class="modal-header">
          <h5 class="modal-title">Editar Curso</h5>
          <button type="button" class="btn-close" @click="emit('close')"></button>
        </div>
        <div class="modal-body">
          <div v-if="loading" class="text-center py-4">
            <div class="spinner-border"></div>
          </div>
          <form v-else @submit.prevent="save" id="editCourseForm">
            <div class="mb-3">
              <label class="form-label">Nombre</label>
              <input v-model="form.nombre" type="text" class="form-control" required />
            </div>
            <div class="mb-3">
              <label class="form-label">Descripción</label>
              <textarea v-model="form.descripcion" class="form-control" rows="3"></textarea>
            </div>
            <div class="mb-3">
              <label class="form-label">Profesor</label>
              <select v-model.number="form.idProfesor" class="form-select" required>
                <option v-for="p in profesores" :key="p.idUsuario" :value="p.idUsuario">
                  {{ p.nombre }} {{ p.apellidos }}
                </option>
              </select>
            </div>
            <div class="mb-3">
              <label class="form-label">Suplente (opcional)</label>
              <select v-model="form.idSuplente" class="form-select">
                <option value="">-- Sin suplente --</option>
                <option v-for="p in profesores" :key="p.idUsuario" :value="p.idUsuario">
                  {{ p.nombre }} {{ p.apellidos }}
                </option>
              </select>
            </div>
            <div class="row mb-3">
              <div class="col-md-6">
                <label class="form-label">Fecha inicio</label>
                <input v-model="form.fechaInicio" type="date" class="form-control" required />
              </div>
              <div class="col-md-6">
                <label class="form-label">Fecha fin</label>
                <input v-model="form.fechaFin" type="date" class="form-control" required />
              </div>
            </div>
            <div class="mb-3">
              <label class="form-label">Estado</label>
              <select v-model="form.estado" class="form-select" required>
                <option v-for="e in ESTADOS" :key="e">{{ e }}</option>
              </select>
            </div>
            <FileUploader label="Imagen portada (opcional)" accept="image/*" @change="f => coverFile = f" />
          </form>
        </div>
        <div class="modal-footer">
          <button type="button" class="btn btn-secondary" @click="emit('close')">Cancelar</button>
          <button type="submit" form="editCourseForm" class="btn btn-primary" :disabled="saving">Guardar</button>
        </div>
      </div>
    </div>
  </div>
</template>
