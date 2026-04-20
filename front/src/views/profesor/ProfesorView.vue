<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useAuthStore } from '../../stores/auth'
import { formatDate } from '../../utils/format'
import { getCourses } from '../../api/courses'
import { getSubjectsByProfessor } from '../../api/subjects'
import type { VistaCursosDetalles, VistaAsignaturasProfesor } from '../../types'

const auth = useAuthStore()
const activeTab = ref<'cursos' | 'asignaturas'>('cursos')
const cursos = ref<VistaCursosDetalles[]>([])
const asignaturas = ref<VistaAsignaturasProfesor[]>([])
const loading = ref(true)

onMounted(async () => {
  try {
    const [c, a] = await Promise.all([
      getCourses(),
      getSubjectsByProfessor(auth.user!.id),
    ])
    cursos.value = c.data
    asignaturas.value = a.data
    if (!cursos.value.length && asignaturas.value.length) activeTab.value = 'asignaturas'
  } finally {
    loading.value = false
  }
})
</script>

<template>
  <main class="container pt-5">
    <div class="tabs">
      <div class="tab" :class="{ active: activeTab === 'cursos' }" @click="activeTab = 'cursos'">Mis Cursos</div>
      <div class="tab" :class="{ active: activeTab === 'asignaturas' }" @click="activeTab = 'asignaturas'">Mis Asignaturas</div>
    </div>

    <div v-if="loading" class="text-center py-5"><div class="spinner-border"></div></div>

    <!-- Tab Cursos -->
    <div v-else-if="activeTab === 'cursos'">
      <div v-if="cursos.length" class="courses-grid">
        <div class="course-card" v-for="curso in cursos" :key="curso.idCurso">
          <div class="course-image">
            <img :src="`/assets/images/courses/${curso.imagenPortada}`" :alt="curso.nombreCurso" class="img-fluid w-100 h-100" style="object-fit:cover" />
          </div>
          <div class="course-header">
            <h3 class="course-title">{{ curso.nombreCurso }}</h3>
            <span class="course-status" :class="curso.estado === 'Activo' ? 'status-active' : 'status-inactive'">{{ curso.estado }}</span>
          </div>
          <div class="course-content">
            <div class="course-info"><i class="fas fa-user-tie me-1"></i> Profesor: {{ curso.nombreProfesor }}</div>
            <div class="course-dates"><i class="far fa-calendar-alt me-1"></i> {{ formatDate(curso.fechaInicio) }} - {{ formatDate(curso.fechaFin) }}</div>
            <div class="course-stats">
              <div class="course-stat"><div class="course-stat-value">{{ curso.numeroAsignaturas }}</div><div class="course-stat-label">Asignaturas</div></div>
              <div class="course-stat"><div class="course-stat-value">{{ curso.numeroAlumnos }}</div><div class="course-stat-label">Alumnos</div></div>
            </div>
          </div>
        </div>
      </div>
      <div v-else class="course-content-container">
        <div class="add-content-container"><p class="add-content-text">No eres tutor o suplente de ningún curso.</p></div>
      </div>
    </div>

    <!-- Tab Asignaturas -->
    <div v-else>
      <div v-if="asignaturas.length" class="courses-grid">
        <div class="course-card asignatura-card" v-for="asig in asignaturas" :key="asig.idAsignatura">
          <div class="course-image" style="background:linear-gradient(135deg,#3498db,#2c3e50)"></div>
          <div class="course-header">
            <h3 class="course-title">{{ asig.nombreAsignatura }}</h3>
            <span class="course-status" :class="asig.estado === 'Activo' ? 'status-active' : 'status-inactive'">{{ asig.estado }}</span>
          </div>
          <div class="course-content">
            <div class="course-info"><i class="fas fa-book me-1"></i> {{ asig.nombreCurso }}</div>
            <div class="course-dates"><i class="far fa-calendar-alt me-1"></i> {{ formatDate(asig.fechaInicio) }} - {{ formatDate(asig.fechaFin) }}</div>
            <div class="course-stats">
              <div class="course-stat"><div class="course-stat-value">{{ asig.numeroTemas }}</div><div class="course-stat-label">Temas</div></div>
              <div class="course-stat"><div class="course-stat-value">{{ asig.numeroContenidos }}</div><div class="course-stat-label">Contenidos</div></div>
            </div>
            <div class="course-actions">
              <RouterLink :to="`/asignatura/${asig.idAsignatura}`" class="btn-view">
                <i class="fas fa-eye me-1"></i> Ver asignatura
              </RouterLink>
            </div>
          </div>
        </div>
      </div>
      <div v-else class="course-content-container">
        <div class="add-content-container"><p class="add-content-text">No eres profesor de ninguna asignatura.</p></div>
      </div>
    </div>
  </main>
</template>

<style scoped>
.tabs { display: flex; margin-bottom: 20px; border-bottom: 1px solid #ccc; }
.tab { padding: 10px 20px; cursor: pointer; margin-right: 5px; }
.tab.active { font-weight: bold; border-bottom: 2px solid #0066cc; }
.courses-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(300px, 1fr)); gap: 20px; margin-top: 20px; }
.course-card { border: 1px solid #e0e0e0; border-radius: 8px; overflow: hidden; transition: box-shadow 0.3s; }
.course-card:hover { box-shadow: 0 5px 15px rgba(0,0,0,0.1); }
.course-image { height: 140px; overflow: hidden; border-bottom: 1px solid #e0e0e0; }
.course-header { padding: 15px; background: #f8f9fa; border-bottom: 1px solid #e0e0e0; }
.course-title { margin: 0; font-size: 18px; font-weight: 600; color: #333; }
.course-status { display: inline-block; padding: 3px 8px; border-radius: 4px; font-size: 12px; }
.status-active { background: #d4edda; color: #155724; }
.status-inactive { background: #f8d7da; color: #721c24; }
.course-content { padding: 15px; }
.course-info, .course-dates { font-size: 14px; color: #666; margin-bottom: 5px; }
.course-stats { display: flex; justify-content: space-between; border-top: 1px solid #e0e0e0; padding-top: 10px; }
.course-stat { text-align: center; }
.course-stat-value { font-size: 18px; font-weight: 600; color: #0066cc; }
.course-stat-label { font-size: 12px; color: #666; }
.course-actions { margin-top: 15px; text-align: right; }
.btn-view { padding: 6px 12px; background: #0066cc; color: white; border-radius: 4px; text-decoration: none; }
.btn-view:hover { background: #0056b3; color: white; }
.course-content-container { padding: 50px 0; text-align: center; }
.add-content-container { padding: 20px; background: #f8f9fa; border-radius: 8px; max-width: 600px; margin: 0 auto; }
.add-content-text { color: #666; }
</style>
