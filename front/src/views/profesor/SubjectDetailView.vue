<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { getSubject } from '../../api/subjects'
import { createTopic } from '../../api/topics'
import type { AsignaturaDetalle } from '../../types'
import ContentForm from '../../components/ContentForm.vue'

const route = useRoute()
const auth = useAuthStore()
const asignatura = ref<AsignaturaDetalle | null>(null)
const loading = ref(true)
const openTemas = ref<Set<number>>(new Set())
const addingContentFor = ref<number | null>(null)
const showTopicForm = ref(false)
const newTopic = ref({ nombre: '', orden: 1 })
const savingTopic = ref(false)

const canEdit = ['Profesor', 'Tutor', 'Administrador'].includes(auth.role) || auth.isAdmin

async function load() {
  try {
    const res = await getSubject(Number(route.params.id))
    asignatura.value = res.data
    if (res.data.temas.length) openTemas.value.add(res.data.temas[0].idTema!)
  } finally {
    loading.value = false
  }
}

onMounted(load)

function toggleTema(id: number) {
  openTemas.value.has(id) ? openTemas.value.delete(id) : openTemas.value.add(id)
}

function contentIcon(tipo: string) {
  const icons: Record<string, string> = {
    Video: 'fa-video', Documento: 'fa-file-alt', Enlace: 'fa-link',
    Tarea: 'fa-laptop-code', Examen: 'fa-circle-question',
  }
  return `fas ${icons[tipo] ?? 'fa-book'}`
}

function contentRoute(idContenido: number, tipo: string) {
  const map: Record<string, string> = {
    Video: 'video', Documento: 'documento', Enlace: 'enlace', Tarea: 'tarea', Examen: 'examen',
  }
  return `/contenido/${idContenido}/${map[tipo] ?? 'documento'}`
}

async function saveTopic() {
  if (!asignatura.value) return
  savingTopic.value = true
  try {
    await createTopic({ ...newTopic.value, idAsignatura: asignatura.value.idAsignatura })
    showTopicForm.value = false
    newTopic.value = { nombre: '', orden: 1 }
    await load()
  } finally {
    savingTopic.value = false
  }
}
</script>

<template>
  <div v-if="loading" class="d-flex justify-content-center py-5"><div class="spinner-border"></div></div>
  <div v-else-if="asignatura" class="main-container">
    <!-- Sidebar -->
    <div class="left-sidebar">
      <div v-for="tema in asignatura.temas" :key="tema.idTema">
        <div class="theme-header" @click="toggleTema(tema.idTema!)">
          <span>{{ tema.nombreTema }}</span>
          <div class="toggle-icon">{{ openTemas.has(tema.idTema!) ? '-' : '+' }}</div>
        </div>
        <div class="theme-content" :class="{ open: openTemas.has(tema.idTema!) }">
          <div
            v-for="c in tema.contenidos" :key="c.idContenido"
            class="theme-item"
            @click="$router.push(contentRoute(c.idContenido!, c.tipoContenido))"
          >
            <i :class="contentIcon(c.tipoContenido)" class="me-1"></i>
            {{ c.tituloContenido }}
          </div>
        </div>
      </div>
    </div>

    <!-- Main content -->
    <div class="main-content">
      <div class="subject-header">
        <h1 class="subject-title">{{ asignatura.nombreAsignatura }}</h1>
        <div class="teachers-container">
          <div class="teacher-info" v-for="p in asignatura.profesores" :key="p.idProfesor">
            <div class="teacher-avatar">
              <img v-if="p.fotoPerfil" :src="`/assets/images/users/${p.fotoPerfil}`" />
              <span v-else>{{ p.nombreProfesor[0] }}</span>
            </div>
            <span class="teacher-name">{{ p.nombreProfesor }} {{ p.apellidosProfesor }}</span>
          </div>
        </div>
      </div>

      <div v-for="tema in asignatura.temas" :key="tema.idTema" class="mb-4">
        <div v-if="openTemas.has(tema.idTema!)" class="theme-content-container">
          <h4 class="px-3 pt-3">{{ tema.nombreTema }}</h4>
          <div v-for="c in tema.contenidos" :key="c.idContenido">
            <RouterLink :to="contentRoute(c.idContenido!, c.tipoContenido)" class="content-item text-decoration-none">
              <div class="content-icon"><i :class="contentIcon(c.tipoContenido)"></i></div>
              <div class="content-info">
                <div class="content-title">{{ c.tituloContenido }}</div>
                <div class="content-description text-muted small">{{ c.descripcionContenido }}</div>
              </div>
              <button class="status-button" :class="c.contenido_Completado ? 'status-done' : 'status-not-done'">
                <i :class="c.contenido_Completado ? 'fas fa-check me-1' : 'fas fa-clock me-1'"></i>
                {{ c.contenido_Completado ? 'Completado' : 'Pendiente' }}
              </button>
            </RouterLink>
          </div>

          <div class="add-content-container p-3" v-if="canEdit">
            <div v-if="addingContentFor === tema.idTema">
              <ContentForm
                :id-tema="tema.idTema!"
                :id-asignatura="asignatura.idAsignatura"
                @saved="() => { addingContentFor = null; load() }"
                @cancel="addingContentFor = null"
              />
            </div>
            <button v-else class="add-button-card" @click="addingContentFor = tema.idTema!">
              <i class="fas fa-plus me-1"></i> Añadir nuevo contenido
            </button>
          </div>
        </div>
      </div>

      <div v-if="canEdit" class="mt-4">
        <div v-if="showTopicForm" class="card p-3" style="max-width:400px">
          <h5>Nuevo tema</h5>
          <div class="mb-2">
            <label class="form-label">Nombre</label>
            <input v-model="newTopic.nombre" type="text" class="form-control" required />
          </div>
          <div class="mb-3">
            <label class="form-label">Orden</label>
            <input v-model.number="newTopic.orden" type="number" class="form-control" min="1" />
          </div>
          <div class="d-flex gap-2">
            <button class="btn btn-secondary" @click="showTopicForm = false">Cancelar</button>
            <button class="btn btn-primary" @click="saveTopic" :disabled="savingTopic">Guardar</button>
          </div>
        </div>
        <button v-else class="add-button-gradient" @click="showTopicForm = true">
          <i class="fas fa-plus me-1"></i> Añadir nuevo tema
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.main-container {
  display: flex;
  width: 100%;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 6px 20px rgba(0,0,0,0.1);
  background: white;
  margin: 20px auto;
  max-width: 1400px;
}
.left-sidebar {
  width: 280px;
  background: white;
  border-right: 1px solid #e0e0e0;
  height: calc(100vh - 100px);
  overflow-y: auto;
}
.theme-header {
  display: flex; justify-content: space-between; align-items: center;
  padding: 15px 20px; border-bottom: 1px solid #eee;
  font-weight: 600; cursor: pointer; transition: all 0.3s;
}
.theme-header:hover { background: #f5f7fb; }
.toggle-icon {
  width: 24px; height: 24px; display: flex; align-items: center; justify-content: center;
  border-radius: 50%; background: #f0f2f5; color: #4361ee;
}
.theme-content { max-height: 0; overflow: hidden; transition: max-height 0.4s; }
.theme-content.open { max-height: 500px; }
.theme-item {
  padding: 12px 20px 12px 40px; cursor: pointer;
  border-bottom: 1px solid #f0f0f0; color: #6c757d;
  transition: all 0.3s; font-size: 14px;
}
.theme-item:hover { background: #f5f7fb; color: #4361ee; }
.main-content {
  flex: 1; padding: 30px;
  overflow-y: auto; height: calc(100vh - 100px);
}
.subject-header {
  margin-bottom: 30px; background: linear-gradient(135deg, #e0f7fa, #bbdefb);
  border-radius: 12px; padding: 30px;
}
.subject-title { font-size: 2.5rem; font-weight: 700; margin-bottom: 15px; }
.teachers-container { display: flex; flex-wrap: wrap; gap: 10px; }
.teacher-info {
  display: flex; align-items: center; background: white;
  padding: 6px 12px; border-radius: 30px; font-size: 14px;
}
.teacher-avatar {
  width: 30px; height: 30px; border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  margin-right: 8px; background: #4361ee; color: white; font-size: 12px;
}
.teacher-avatar img { width: 100%; border-radius: 50%; }
.content-item {
  display: flex; align-items: center; padding: 12px 20px;
  border-bottom: 1px solid #f0f0f0; cursor: pointer;
  transition: background 0.2s; color: inherit;
}
.content-item:hover { background: #f5f7fb; }
.content-icon { width: 36px; height: 36px; display: flex; align-items: center; justify-content: center; margin-right: 12px; color: #4361ee; }
.content-info { flex: 1; }
.content-title { font-weight: 500; }
.status-button { padding: 4px 10px; border-radius: 20px; border: none; font-size: 12px; cursor: default; }
.status-done { background: #d4edda; color: #155724; }
.status-not-done { background: #fff3cd; color: #856404; }
.add-content-container { border-top: 1px dashed #e0e0e0; }
.add-button-card, .add-button-gradient {
  display: flex; align-items: center; gap: 8px;
  padding: 10px 16px; border-radius: 8px; border: 1px dashed #4361ee;
  background: transparent; color: #4361ee; cursor: pointer;
  font-size: 14px; transition: all 0.2s;
}
.add-button-card:hover, .add-button-gradient:hover { background: #f0f4ff; }
</style>
