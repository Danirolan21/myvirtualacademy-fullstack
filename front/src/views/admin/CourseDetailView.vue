<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { getCourse } from '../../api/courses'
import { formatDate } from '../../utils/format'

const route = useRoute()
const curso = ref<any>(null)
const loading = ref(true)

onMounted(async () => {
  try {
    const res = await getCourse(Number(route.params.id))
    curso.value = res.data
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <main class="container mt-4">
    <div v-if="loading" class="text-center py-5">
      <div class="spinner-border"></div>
    </div>
    <template v-else-if="curso">
      <div class="d-flex justify-content-between align-items-center mb-4">
        <h1>{{ curso.nombreCurso }}</h1>
        <RouterLink to="/admin" class="btn btn-outline-secondary">
          <i class="fas fa-arrow-left me-1"></i> Volver
        </RouterLink>
      </div>
      <div class="card mb-4">
        <div class="card-body">
          <div class="row">
            <div class="col-md-4" v-if="curso.imagenPortada">
              <img :src="`/assets/images/courses/${curso.imagenPortada}`" class="img-fluid rounded" />
            </div>
            <div :class="curso.imagenPortada ? 'col-md-8' : 'col-12'">
              <p><strong>Estado:</strong> <span class="badge" :class="`bg-${curso.estado === 'Activo' ? 'success' : 'secondary'}`">{{ curso.estado }}</span></p>
              <p v-if="curso.descripcion"><strong>Descripción:</strong> {{ curso.descripcion }}</p>
              <p><strong>Profesor:</strong> {{ curso.nombreProfesor }}</p>
              <p v-if="curso.nombreSuplente"><strong>Suplente:</strong> {{ curso.nombreSuplente }}</p>
              <p><strong>Inicio:</strong> {{ formatDate(curso.fechaInicio) }}</p>
              <p><strong>Fin:</strong> {{ formatDate(curso.fechaFin) }}</p>
              <p><strong>Asignaturas:</strong> {{ curso.numeroAsignaturas }}</p>
              <p><strong>Alumnos:</strong> {{ curso.numeroAlumnos }}</p>
            </div>
          </div>
        </div>
      </div>
    </template>
  </main>
</template>
