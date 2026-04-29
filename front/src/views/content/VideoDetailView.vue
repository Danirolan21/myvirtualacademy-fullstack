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

const isExternalVideo = computed(() => {
  const url = recurso.value?.urlContenido ?? ''
  return url.startsWith('http://') || url.startsWith('https://')
})

const embedUrl = computed(() => {
  const url = recurso.value?.urlContenido ?? ''
  const yt = url.match(/(?:youtube\.com\/watch\?v=|youtu\.be\/)([^&?/]+)/)
  if (yt) return `https://www.youtube.com/embed/${yt[1]}`
  return url
})
</script>

<template>
  <ContentDetailShell
    :loading="loading"
    :access-denied="accessDenied"
    :titulo="recurso?.titulo"
    badge-label="Video"
    icon-class="fas fa-video"
    icon-wrap-class="content-icon-video"
    :fecha-publicacion="recurso?.fechaPublicacion"
    :breadcrumb-subject-id="recurso?.idAsignatura"
    :breadcrumb-subject-name="recurso?.nombreAsignatura"
  >
    <div v-if="recurso?.descripcion" class="description-block">
      <h5 class="block-title">Descripción</h5>
      <p class="block-text">{{ recurso.descripcion }}</p>
    </div>

    <div class="video-wrap">
      <iframe v-if="isExternalVideo" :src="embedUrl" allowfullscreen></iframe>
      <video v-else controls :src="`/uploads/contents/${recurso?.urlContenido}`"></video>
    </div>
  </ContentDetailShell>
</template>

<style scoped>
.content-icon-video { background: #fee2e2; color: #dc2626; }

.description-block {}
.block-title { font-size: var(--font-size-md); font-weight: var(--font-weight-semibold); margin: 0 0 var(--sp-2); color: var(--color-text); }
.block-text { font-size: var(--font-size-sm); color: var(--color-muted); margin: 0; }

.video-wrap {
  position: relative;
  padding-top: 56.25%;
  border-radius: var(--radius-md);
  overflow: hidden;
  background: #000;
}
.video-wrap iframe { position: absolute; inset: 0; width: 100%; height: 100%; border: none; }
.video-wrap video { position: absolute; inset: 0; width: 100%; height: 100%; }
</style>
