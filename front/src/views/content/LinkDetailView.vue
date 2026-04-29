<script setup lang="ts">
import { ref, onMounted } from 'vue'
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
</style>
