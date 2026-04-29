<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { getContent, checkAccess, registerView } from '../../api/content'
import type { RecursoViewModel } from '../../types'
import ContentDetailShell from '../../components/ContentDetailShell.vue'

const route = useRoute()
const recurso = ref<RecursoViewModel | null>(null)
const loading = ref(true)
const accessDenied = ref(false)

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
</style>
