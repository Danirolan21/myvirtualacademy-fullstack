<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { getContent, checkAccess, registerView } from '../../api/content'
import { formatDateTime } from '../../utils/format'
import type { RecursoViewModel } from '../../types'

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
  <div class="page">
    <div class="content-container">
      <div v-if="loading" class="loading-center"><div class="spinner"></div></div>

      <div v-else-if="accessDenied" class="access-denied">
        <i class="fas fa-lock access-denied-icon"></i>
        <h2>Acceso restringido</h2>
        <p>No estás inscrito en el curso al que pertenece este contenido.</p>
        <button class="btn btn-outline-secondary" @click="$router.back()">
          <i class="fas fa-arrow-left"></i> Volver
        </button>
      </div>

      <template v-else-if="recurso">
        <nav class="breadcrumb">
          <RouterLink to="/" class="breadcrumb-item">Inicio</RouterLink>
          <span class="breadcrumb-sep"><i class="fas fa-chevron-right"></i></span>
          <RouterLink :to="`/asignatura/${recurso.idAsignatura}`" class="breadcrumb-item">{{ recurso.nombreAsignatura }}</RouterLink>
          <span class="breadcrumb-sep"><i class="fas fa-chevron-right"></i></span>
          <span class="breadcrumb-item breadcrumb-active">{{ recurso.titulo }}</span>
        </nav>

        <div class="content-card">
          <div class="content-header">
            <div class="content-header-left">
              <div class="content-icon-wrap content-icon-video">
                <i class="fas fa-video"></i>
              </div>
              <h1 class="content-title">{{ recurso.titulo }}</h1>
            </div>
            <span class="badge badge-secondary">Video</span>
          </div>

          <div class="content-body">
            <p class="pub-date"><i class="fas fa-calendar-alt"></i> Publicado el {{ formatDateTime(recurso.fechaPublicacion) }}</p>

            <div v-if="recurso.descripcion" class="description-block">
              <h5 class="block-title">Descripción</h5>
              <p class="block-text">{{ recurso.descripcion }}</p>
            </div>

            <div class="video-wrap">
              <iframe v-if="isExternalVideo" :src="embedUrl" allowfullscreen></iframe>
              <video v-else controls :src="`/uploads/contents/${recurso.urlContenido}`"></video>
            </div>
          </div>

          <div class="content-footer">
            <button class="btn btn-outline-secondary" @click="$router.back()">
              <i class="fas fa-arrow-left"></i> Volver
            </button>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.content-container {
  max-width: 800px;
  margin: 0 auto;
  padding: var(--sp-6) var(--sp-4);
}
.loading-center { display: flex; justify-content: center; padding: var(--sp-12) 0; }

.breadcrumb { display: flex; align-items: center; gap: var(--sp-2); flex-wrap: wrap; margin-bottom: var(--sp-4); font-size: var(--font-size-sm); }
.breadcrumb-item { color: var(--color-muted); text-decoration: none; transition: color 0.15s; }
.breadcrumb-item:hover { color: var(--color-text); }
.breadcrumb-active { color: var(--color-text); font-weight: var(--font-weight-medium); }
.breadcrumb-sep { color: var(--color-muted); font-size: 10px; }

.content-card { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg); overflow: hidden; box-shadow: var(--shadow-sm); }

.content-header {
  padding: var(--sp-5) var(--sp-6);
  border-bottom: 1px solid var(--color-border);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--sp-4);
}
.content-header-left { display: flex; align-items: center; gap: var(--sp-4); flex: 1; min-width: 0; }
.content-icon-wrap {
  width: 44px; height: 44px;
  border-radius: var(--radius-md);
  display: flex; align-items: center; justify-content: center;
  font-size: var(--font-size-xl);
  flex-shrink: 0;
}
.content-icon-video { background: #fee2e2; color: #dc2626; }
.content-title { font-size: var(--font-size-xl); font-weight: var(--font-weight-bold); color: var(--color-text); margin: 0; }

.content-body { padding: var(--sp-6); display: flex; flex-direction: column; gap: var(--sp-5); }
.content-footer { padding: var(--sp-4) var(--sp-6); border-top: 1px solid var(--color-border); background: var(--color-muted-bg); }

.pub-date { font-size: var(--font-size-sm); color: var(--color-muted); display: flex; align-items: center; gap: var(--sp-2); margin: 0; }

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

.access-denied {
  display: flex; flex-direction: column; align-items: center; justify-content: center;
  text-align: center; padding: var(--sp-16) var(--sp-4); color: var(--color-muted);
}
.access-denied-icon { font-size: 3rem; color: var(--color-danger); opacity: 0.6; margin-bottom: var(--sp-4); }
.access-denied h2 { color: var(--color-text); margin: 0 0 var(--sp-2); }
.access-denied p { margin: 0 0 var(--sp-6); }
</style>
