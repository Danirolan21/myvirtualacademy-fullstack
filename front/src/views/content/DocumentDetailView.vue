<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { getContent, checkAccess, registerView, updateContent } from '../../api/content'
import type { RecursoViewModel } from '../../types'
import ContentDetailShell from '../../components/ContentDetailShell.vue'
import FileUploader from '../../components/FileUploader.vue'

const route = useRoute()
const auth = useAuthStore()
const recurso = ref<RecursoViewModel | null>(null)
const loading = ref(true)
const accessDenied = ref(false)

const canEdit = computed(() => auth.isAdmin || ['Profesor', 'Tutor', 'Administrador'].includes(auth.role))

const showEditModal = ref(false)
const editForm = ref({ titulo: '', descripcion: '' })
const editFile = ref<File | null>(null)
const saving = ref(false)

onMounted(async () => {
  try {
    const hasAccess = await checkAccess(Number(route.params.id))
    if (!hasAccess) { accessDenied.value = true; return }
    const res = await getContent(Number(route.params.id))
    recurso.value = res.data
    await registerView(Number(route.params.id))
  } finally {
    loading.value = false
  }
})

function openEditModal() {
  if (!recurso.value) return
  editForm.value = { titulo: recurso.value.titulo, descripcion: recurso.value.descripcion ?? '' }
  editFile.value = null
  showEditModal.value = true
}

async function saveEdit() {
  saving.value = true
  try {
    const fd = new FormData()
    fd.append('titulo', editForm.value.titulo)
    fd.append('descripcion', editForm.value.descripcion)
    if (editFile.value) fd.append('archivoContenido', editFile.value)
    await updateContent(Number(route.params.id), fd)
    showEditModal.value = false
    const res = await getContent(Number(route.params.id))
    recurso.value = res.data
  } finally {
    saving.value = false
  }
}

const fileExt = computed(() => {
  const name = recurso.value?.urlContenido ?? ''
  return name.split('.').pop()?.toLowerCase() ?? ''
})

const fileIcon = computed(() => {
  const map: Record<string, string> = {
    pdf: 'fa-file-pdf', doc: 'fa-file-word', docx: 'fa-file-word',
    xls: 'fa-file-excel', xlsx: 'fa-file-excel',
    ppt: 'fa-file-powerpoint', pptx: 'fa-file-powerpoint',
    zip: 'fa-file-archive', rar: 'fa-file-archive',
    jpg: 'fa-file-image', jpeg: 'fa-file-image', png: 'fa-file-image', gif: 'fa-file-image',
  }
  return `fas ${map[fileExt.value] ?? 'fa-file-alt'}`
})

const fileIconColor = computed(() => {
  if (['pdf'].includes(fileExt.value)) return 'icon-pdf'
  if (['doc', 'docx'].includes(fileExt.value)) return 'icon-word'
  if (['xls', 'xlsx'].includes(fileExt.value)) return 'icon-excel'
  if (['ppt', 'pptx'].includes(fileExt.value)) return 'icon-ppt'
  return 'icon-default'
})
</script>

<template>
  <ContentDetailShell
    :loading="loading"
    :access-denied="accessDenied"
    :titulo="recurso?.titulo"
    badge-label="Documento"
    icon-class="fas fa-file-alt"
    icon-wrap-class="content-icon-doc"
    :fecha-publicacion="recurso?.fechaPublicacion"
    :breadcrumb-subject-id="recurso?.idAsignatura"
    :breadcrumb-subject-name="recurso?.nombreAsignatura"
  >
    <button v-if="canEdit" class="btn btn-sm btn-outline-secondary edit-btn" @click="openEditModal">
      <i class="fas fa-edit"></i> Editar
    </button>

    <div v-if="recurso?.descripcion" class="description-block">
      <h5 class="block-title">Descripción</h5>
      <p class="block-text">{{ recurso.descripcion }}</p>
    </div>

    <div class="file-preview">
      <i :class="[fileIcon, fileIconColor, 'file-big-icon']"></i>
      <div class="file-name">{{ recurso?.urlContenido?.split('/').pop() }}</div>
      <div class="file-ext-badge">{{ fileExt.toUpperCase() }}</div>
    </div>

    <div class="download-actions">
      <a :href="`/uploads/contents/${recurso?.urlContenido}`" download class="btn btn-primary">
        <i class="fas fa-download"></i> Descargar documento
      </a>
      <a :href="`/uploads/contents/${recurso?.urlContenido}`" target="_blank" rel="noopener" class="btn btn-outline-secondary">
        <i class="fas fa-eye"></i> Ver documento
      </a>
    </div>
  </ContentDetailShell>

  <Teleport to="body">
    <div v-if="showEditModal" class="modal-overlay" @mousedown.self="showEditModal = false">
      <div class="modal-dialog">
        <div class="modal-header">
          <h5 class="modal-title"><i class="fas fa-edit"></i> Editar Documento</h5>
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
          <div class="field">
            <label class="form-label">Archivo</label>
            <FileUploader label="Reemplazar documento" @change="f => editFile = f" />
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
.content-icon-doc { background: #dbeafe; color: var(--color-primary); }

.description-block {}
.block-title { font-size: var(--font-size-md); font-weight: var(--font-weight-semibold); margin: 0 0 var(--sp-2); color: var(--color-text); }
.block-text { font-size: var(--font-size-sm); color: var(--color-muted); margin: 0; }

.file-preview {
  display: flex; flex-direction: column; align-items: center; gap: var(--sp-3);
  background: var(--color-muted-bg); border: 1px solid var(--color-border);
  border-radius: var(--radius-lg); padding: var(--sp-8) var(--sp-4);
}
.file-big-icon { font-size: 4rem; }
.icon-pdf { color: #dc2626; }
.icon-word { color: #2563eb; }
.icon-excel { color: #16a34a; }
.icon-ppt { color: #d97706; }
.icon-default { color: var(--color-muted); }
.file-name { font-size: var(--font-size-sm); font-weight: var(--font-weight-medium); color: var(--color-text); word-break: break-all; text-align: center; }
.file-ext-badge { background: var(--color-border); color: var(--color-muted); border-radius: var(--radius-sm); padding: 2px 8px; font-size: var(--font-size-xs); font-weight: var(--font-weight-semibold); text-transform: uppercase; }

.download-actions { display: flex; flex-direction: column; gap: var(--sp-3); }

.edit-btn { align-self: flex-start; }
.field { display: flex; flex-direction: column; gap: var(--sp-1); }
.req { color: var(--color-danger); }
.modal-header { display: flex; align-items: center; justify-content: space-between; padding: var(--sp-5) var(--sp-6); border-bottom: 1px solid var(--color-border); }
.modal-title { font-size: var(--font-size-lg); font-weight: var(--font-weight-semibold); margin: 0; display: flex; align-items: center; gap: var(--sp-2); color: var(--color-text); }
.modal-close { background: none; border: none; font-size: var(--font-size-md); cursor: pointer; padding: var(--sp-1); transition: color 0.15s; }
.modal-body { padding: var(--sp-5) var(--sp-6); display: flex; flex-direction: column; gap: var(--sp-4); }
.modal-footer { display: flex; justify-content: flex-end; gap: var(--sp-3); padding: var(--sp-4) var(--sp-6); border-top: 1px solid var(--color-border); }
</style>
