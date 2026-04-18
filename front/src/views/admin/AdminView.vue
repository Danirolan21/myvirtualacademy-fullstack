<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getCourses } from '../../api/courses'
import type { VistaCursosDetalles } from '../../types'
import CourseCard from '../../components/CourseCard.vue'
import CourseEditModal from '../../components/CourseEditModal.vue'

const courses = ref<VistaCursosDetalles[]>([])
const loading = ref(true)
const editingId = ref<number | null>(null)

async function load() {
  loading.value = true
  try {
    const res = await getCourses()
    courses.value = res.data
  } finally {
    loading.value = false
  }
}

onMounted(load)

function onSaved() {
  editingId.value = null
  load()
}
</script>

<template>
  <main class="container">
    <div class="tabs justify-content-between">
      <div class="tab active">Cursos</div>
      <RouterLink to="/admin/cursos/crear" class="btn btn-primary">
        <i class="fas fa-plus me-1"></i> Crear Nuevo Curso
      </RouterLink>
    </div>

    <div v-if="loading" class="text-center py-5">
      <div class="spinner-border"></div>
    </div>
    <div v-else-if="courses.length" class="courses-grid mt-3">
      <CourseCard
        v-for="curso in courses"
        :key="curso.idCurso"
        :curso="curso"
        :admin-mode="true"
        @options="id => editingId = id"
      />
    </div>
    <div v-else class="course-content-container">
      <div class="add-content-container">
        <p class="add-content-text">Aún no hay cursos creados.</p>
      </div>
    </div>

    <CourseEditModal :id-curso="editingId" @saved="onSaved" @close="editingId = null" />
  </main>
</template>

<style scoped>
.tabs {
  display: flex;
  border-bottom: 2px solid #e0e0e0;
  margin-bottom: 1.5rem;
  align-items: center;
  padding: 1rem 0 0;
}
.tab { padding: 0.75rem 1.25rem; font-weight: 500; border-bottom: 2px solid black; }
.courses-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}
</style>
