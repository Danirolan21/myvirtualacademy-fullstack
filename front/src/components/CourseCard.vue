<script setup lang="ts">
import type { VistaCursosDetalles } from '../types'

defineProps<{ curso: VistaCursosDetalles; adminMode?: boolean }>()
const emit = defineEmits<{ (e: 'options', id: number): void }>()

const statusClass: Record<string, string> = {
  Activo: 'status-active',
  Borrador: 'status-draft',
  Finalizado: 'status-complete',
  Archivado: 'status-archived',
  Suspendido: 'status-suspended',
}
</script>

<template>
  <div class="course-card">
    <RouterLink class="text-dark" :to="`/admin/cursos/${curso.idCurso}`">
      <div class="course-image" :style="{ backgroundImage: `url('/assets/images/courses/${curso.imagenPortada}')` }">
        <span class="course-status" :class="statusClass[curso.estado] ?? ''">{{ curso.estado }}</span>
        <div class="course-actions" v-if="adminMode">
          <button type="button" class="btn-options" @click.prevent="emit('options', curso.idCurso)">
            <i class="fas fa-ellipsis-v"></i>
          </button>
        </div>
      </div>
      <div class="course-content">
        <h3 class="course-title">{{ curso.nombreCurso }}</h3>
        <div class="course-info"><i class="fas fa-user me-1"></i>{{ curso.nombreProfesor }}</div>
        <div class="course-info"><i class="fas fa-calendar me-1"></i>Inicio: {{ new Date(curso.fechaInicio).toLocaleDateString('es-ES') }}</div>
        <div class="course-info"><i class="fas fa-book me-1"></i>Asignaturas: {{ curso.numeroAsignaturas }}</div>
        <div class="course-info"><i class="fas fa-users me-1"></i>Alumnos: {{ curso.numeroAlumnos }}</div>
      </div>
    </RouterLink>
  </div>
</template>
