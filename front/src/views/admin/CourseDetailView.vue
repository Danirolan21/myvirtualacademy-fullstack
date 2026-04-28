<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { getCourse } from '../../api/courses'
import { formatDate } from '../../utils/format'

const route = useRoute()
const curso = ref<any>(null)
const loading = ref(true)
const activeTab = ref<'asignaturas' | 'alumnos' | 'evaluacion' | 'configuracion'>('asignaturas')

onMounted(async () => {
  try {
    const res = await getCourse(Number(route.params.id))
    curso.value = res.data
  } finally {
    loading.value = false
  }
})

const statusClass: Record<string, string> = {
  Activo: 'badge-activo', Borrador: 'badge-borrador',
  Finalizado: 'badge-finalizado', Archivado: 'badge-archivado', Suspendido: 'badge-suspendido',
}
</script>

<template>
  <div class="page">
    <div class="container">

      <div v-if="loading" class="loading-center"><div class="spinner"></div></div>

      <template v-else-if="curso">
        <!-- Back link -->
        <RouterLink to="/admin" class="back-link">
          <i class="fas fa-arrow-left"></i> Volver a cursos
        </RouterLink>

        <!-- Header card -->
        <div class="header-card">
          <div class="header-main">
            <span class="badge" :class="statusClass[curso.curso?.estado ?? 'Borrador']">
              {{ curso.curso?.estado ?? 'Borrador' }}
            </span>
            <h1 class="header-title">{{ curso.curso?.nombreCurso ?? curso.nombreCurso }}</h1>
          </div>
          <div class="header-meta">
            <span><i class="fas fa-user"></i> {{ curso.curso?.nombreProfesor ?? curso.nombreProfesor }}</span>
            <span><i class="fas fa-calendar-alt"></i> Inicio: {{ formatDate(curso.curso?.fechaInicio ?? curso.fechaInicio) }}</span>
            <span><i class="fas fa-book-open"></i> Asignaturas: {{ curso.asignaturas?.length ?? 0 }}</span>
            <span><i class="fas fa-users"></i> Alumnos: {{ curso.curso?.numeroAlumnos ?? 0 }}</span>
          </div>
        </div>

        <!-- Tabs -->
        <div class="tabs-bar">
          <button class="tab-btn" :class="{ active: activeTab === 'asignaturas' }" @click="activeTab = 'asignaturas'">Asignaturas</button>
          <button class="tab-btn" :class="{ active: activeTab === 'alumnos' }" @click="activeTab = 'alumnos'">Alumnos</button>
          <button class="tab-btn" :class="{ active: activeTab === 'evaluacion' }" @click="activeTab = 'evaluacion'">Evaluación</button>
          <button class="tab-btn" :class="{ active: activeTab === 'configuracion' }" @click="activeTab = 'configuracion'">Configuración</button>
        </div>

        <!-- Tab: Asignaturas -->
        <div v-if="activeTab === 'asignaturas'" class="tab-panel">
          <div v-if="curso.asignaturas?.length" class="subject-list">
            <RouterLink
              v-for="asig in curso.asignaturas"
              :key="asig.idAsignatura"
              :to="`/asignatura/${asig.idAsignatura}`"
              class="subject-row"
            >
              <div class="subject-icon"><i class="fas fa-book"></i></div>
              <div class="subject-info">
                <div class="subject-name">{{ asig.nombre ?? asig.nombreAsignatura }}</div>
                <div class="subject-meta" v-if="asig.numeroTemas != null">{{ asig.numeroTemas }} módulos &bull; {{ asig.numeroContenidos ?? 0 }} contenidos</div>
              </div>
              <i class="fas fa-arrow-right subject-arrow"></i>
            </RouterLink>
          </div>
          <div v-else class="empty-tab">No hay asignaturas en este curso.</div>
        </div>

        <div v-else-if="activeTab === 'alumnos'" class="tab-panel">
          <div class="empty-tab">Funcionalidad de alumnos próximamente.</div>
        </div>
        <div v-else-if="activeTab === 'evaluacion'" class="tab-panel">
          <div class="empty-tab">Funcionalidad de evaluación próximamente.</div>
        </div>
        <div v-else class="tab-panel">
          <div class="empty-tab">Funcionalidad de configuración próximamente.</div>
        </div>

      </template>
    </div>
  </div>
</template>

<style scoped>
.back-link {
  display: inline-flex;
  align-items: center;
  gap: var(--sp-2);
  color: var(--color-muted);
  font-size: var(--font-size-sm);
  margin-bottom: var(--sp-5);
  text-decoration: none;
  transition: color 0.15s;
}
.back-link:hover { color: var(--color-text); }

.header-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: var(--sp-5) var(--sp-6);
  margin-bottom: var(--sp-4);
  box-shadow: var(--shadow-sm);
}
.header-main {
  display: flex;
  align-items: center;
  gap: var(--sp-3);
  margin-bottom: var(--sp-3);
}
.header-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  margin: 0;
}
.header-meta {
  display: flex;
  flex-wrap: wrap;
  gap: var(--sp-5);
  font-size: var(--font-size-sm);
  color: var(--color-muted);
}
.header-meta i { margin-right: var(--sp-1); }

.tabs-bar {
  display: flex;
  border-bottom: 1px solid var(--color-border);
  margin-bottom: var(--sp-6);
  gap: 0;
}
.tab-btn {
  padding: var(--sp-3) var(--sp-5);
  background: none;
  border: none;
  border-bottom: 2px solid transparent;
  margin-bottom: -1px;
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-muted);
  cursor: pointer;
  transition: color 0.15s, border-color 0.15s;
}
.tab-btn:hover { color: var(--color-text); }
.tab-btn.active {
  color: var(--color-primary);
  border-bottom-color: var(--color-primary);
  font-weight: var(--font-weight-semibold);
}

.tab-panel { background: var(--color-surface); border: 1px solid var(--color-border); border-radius: var(--radius-lg); overflow: hidden; }

.subject-list { display: flex; flex-direction: column; }
.subject-row {
  display: flex;
  align-items: center;
  gap: var(--sp-4);
  padding: var(--sp-4) var(--sp-5);
  border-bottom: 1px solid var(--color-border);
  text-decoration: none;
  color: var(--color-text);
  transition: background 0.15s;
}
.subject-row:last-child { border-bottom: none; }
.subject-row:hover { background: var(--color-muted-bg); }
.subject-icon {
  width: 32px; height: 32px;
  background: var(--color-muted-bg);
  border-radius: var(--radius-sm);
  display: flex; align-items: center; justify-content: center;
  color: var(--color-muted);
  flex-shrink: 0;
}
.subject-info { flex: 1; }
.subject-name { font-weight: var(--font-weight-medium); font-size: var(--font-size-sm); }
.subject-meta { font-size: var(--font-size-xs); color: var(--color-muted); margin-top: 2px; }
.subject-arrow { color: var(--color-muted); font-size: var(--font-size-sm); }

.empty-tab { padding: var(--sp-10); text-align: center; color: var(--color-muted); font-size: var(--font-size-sm); }
.loading-center { display: flex; justify-content: center; padding: var(--sp-12) 0; }
</style>
