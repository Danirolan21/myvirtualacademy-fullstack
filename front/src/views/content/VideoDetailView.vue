<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { getContent, registerView } from '../../api/content'
import { formatDateTime } from '../../utils/format'
import type { RecursoViewModel } from '../../types'

const route = useRoute()
const recurso = ref<RecursoViewModel | null>(null)
const loading = ref(true)

onMounted(async () => {
  try {
    const res = await getContent(Number(route.params.id))
    recurso.value = res.data
    await registerView(Number(route.params.id))
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <div class="container mt-4">
    <div v-if="loading" class="text-center py-5"><div class="spinner-border"></div></div>
    <template v-else-if="recurso">
      <div class="row">
        <div class="col-md-8 offset-md-2">
          <nav aria-label="breadcrumb">
            <ol class="breadcrumb">
              <li class="breadcrumb-item"><RouterLink to="/">Inicio</RouterLink></li>
              <li class="breadcrumb-item"><RouterLink :to="`/asignatura/${recurso.idAsignatura}`">{{ recurso.nombreAsignatura }}</RouterLink></li>
              <li class="breadcrumb-item active">{{ recurso.titulo }}</li>
            </ol>
          </nav>
          <div class="card shadow-sm">
            <div class="card-header d-flex justify-content-between align-items-center">
              <div class="d-flex align-items-center">
                <i class="fas fa-video text-danger me-3 fs-3"></i>
                <h4 class="mb-0">{{ recurso.titulo }}</h4>
              </div>
              <span class="badge bg-secondary">Video</span>
            </div>
            <div class="card-body">
              <small class="text-muted">Publicado el {{ formatDateTime(recurso.fechaPublicacion) }}</small>
              <div v-if="recurso.descripcion" class="mt-3 mb-4">
                <h5>Descripción</h5>
                <p>{{ recurso.descripcion }}</p>
              </div>
              <div class="ratio ratio-16x9 mt-3">
                <iframe :src="`/uploads/contents/${recurso.urlContenido}`" allowfullscreen></iframe>
              </div>
            </div>
            <div class="card-footer bg-transparent">
              <button class="btn btn-outline-secondary" @click="$router.back()">
                <i class="fas fa-arrow-left me-2"></i>Volver
              </button>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
