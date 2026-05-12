<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { getContent, checkAccess, registerView, updateContent } from '../../api/content'
import type { RecursoViewModel } from '../../types'
import ContentDetailShell from '../../components/ContentDetailShell.vue'

const route = useRoute()
const auth = useAuthStore()
const recurso = ref<RecursoViewModel | null>(null)
const loading = ref(true)
const accessDenied = ref(false)

const canEdit = computed(() => auth.isAdmin || ['Profesor', 'Tutor', 'Administrador'].includes(auth.role))

const showEditModal = ref(false)
const editForm = ref({ titulo: '', descripcion: '', urlContenido: '' })
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
  editForm.value = {
    titulo: recurso.value.titulo,
    descripcion: recurso.value.descripcion ?? '',
    urlContenido: recurso.value.urlContenido ?? '',
  }
  showEditModal.value = true
}

async function saveEdit() {
  saving.value = true
  try {
    const fd = new FormData()
    fd.append('titulo', editForm.value.titulo)
    fd.append('descripcion', editForm.value.descripcion)
    fd.append('urlContenido', editForm.value.urlContenido)
    await updateContent(Number(route.params.id), fd)
    showEditModal.value = false
    const res = await getContent(Number(route.params.id))
    recurso.value = res.data
  } finally {
    saving.value = false
  }
}
</script>

<template>
  <ContentDetailShell
    :loading="loading"
    :access-denied="accessDenied"
    :titulo="recurso?.titulo"
    badge-label="Enlace"
    icon-class="fas fa-link"
    icon-wrap-class="content-icon-link"
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

    <div class="link-preview">
      <div class="link-icon-wrap"><i class="fas fa-external-link-alt"></i></div>
      <div class="link-url">{{ recurso?.urlContenido }}</div>
    </div>

    <a :href="recurso?.urlContenido" target="_blank" rel="noopener noreferrer" class="btn btn-primary">
      <i class="fas fa-external-link-alt"></i> Visitar enlace
    </a>
  </ContentDetailShell>

  <Teleport to="body">
    <div v-if="showEditModal" class="modal-overlay" @mousedown.self="showEditModal = false">
      <div class="modal-dialog">
        <div class="modal-header">
          <h5 class="modal-title"><i class="fas fa-edit"></i> Editar Enlace</h5>
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
            <label class="form-label">URL <span class="req">*</span></label>
            <input v-model="editForm.urlContenido" type="url" class="form-control" required placeholder="https://..." />
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
.content-icon-link { background: #dcfce7; color: #16a34a; }

.description-block {}
.block-title { font-size: var(--font-size-md); font-weight: var(--font-weight-semibold); margin: 0 0 var(--sp-2); color: var(--color-text); }
.block-text { font-size: var(--font-size-sm); color: var(--color-muted); margin: 0; }

.link-preview {
  display: flex; align-items: center; gap: var(--sp-3);
  background: var(--color-muted-bg); border: 1px solid var(--color-border);
  border-radius: var(--radius-md); padding: var(--sp-4);
}
.link-icon-wrap { color: #16a34a; font-size: var(--font-size-xl); flex-shrink: 0; }
.link-url { font-size: var(--font-size-sm); color: var(--color-primary); word-break: break-all; }

.edit-btn { align-self: flex-start; }
.field { display: flex; flex-direction: column; gap: var(--sp-1); }
.req { color: var(--color-danger); }
.modal-header { display: flex; align-items: center; justify-content: space-between; padding: var(--sp-5) var(--sp-6); border-bottom: 1px solid var(--color-border); }
.modal-title { font-size: var(--font-size-lg); font-weight: var(--font-weight-semibold); margin: 0; display: flex; align-items: center; gap: var(--sp-2); color: var(--color-text); }
.modal-close { background: none; border: none; font-size: var(--font-size-md); cursor: pointer; padding: var(--sp-1); transition: color 0.15s; }
.modal-body { padding: var(--sp-5) var(--sp-6); display: flex; flex-direction: column; gap: var(--sp-4); }
.modal-footer { display: flex; justify-content: flex-end; gap: var(--sp-3); padding: var(--sp-4) var(--sp-6); border-top: 1px solid var(--color-border); }
</style>
