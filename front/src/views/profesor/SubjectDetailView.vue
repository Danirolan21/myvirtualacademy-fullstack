<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { getSubject, updateSubject, addProfesorToSubject, removeProfesorFromSubject } from '../../api/subjects'
import { createTopic, deleteTopic } from '../../api/topics'
import { deleteContent } from '../../api/content'
import client from '../../api/client'
import Swal from 'sweetalert2'
import type { AsignaturaDetalle, TemaVM, ContenidoVM } from '../../types'
import ContentForm from '../../components/ContentForm.vue'

const route = useRoute()
const auth = useAuthStore()
const asignatura = ref<AsignaturaDetalle | null>(null)
const loading = ref(true)

// Acordeón exclusivo: solo un tema abierto a la vez
const openTemaId = ref<number | null>(null)
// Contenido seleccionado en el panel derecho
const selectedContent = ref<{ tema: TemaVM; contenido: ContenidoVM } | null>(null)

const addingContentFor = ref<number | null>(null)
const showTopicModal = ref(false)
const mobileSidebarOpen = ref(false)
const newTopic = ref({ nombre: '', orden: 1 })
const savingTopic = ref(false)

const canEdit = computed(() => ['Profesor', 'Tutor', 'Administrador'].includes(auth.role) || auth.isAdmin)

const hasTemas = computed(() => (asignatura.value?.temas?.length ?? 0) > 0)

// Editar nombre de asignatura
const editingName = ref(false)
const editingNameValue = ref('')
const savingName = ref(false)

function startEditName() {
  editingNameValue.value = asignatura.value?.nombreAsignatura ?? ''
  editingName.value = true
}

async function saveEditName() {
  if (!editingNameValue.value.trim() || !asignatura.value) return
  savingName.value = true
  try {
    await updateSubject(asignatura.value.idAsignatura, editingNameValue.value.trim())
    asignatura.value.nombreAsignatura = editingNameValue.value.trim()
    editingName.value = false
  } finally {
    savingName.value = false
  }
}

// Gestión de profesores
interface ProfesorItem { idUsuario: number; nombreCompleto: string }
const profesoresDisponibles = ref<ProfesorItem[]>([])
const showAddProfesor = ref(false)
const selectedProfesorId = ref<number | ''>('')
const addingProfesor = ref(false)

async function loadProfesores() {
  try {
    const res = await client.get<ProfesorItem[]>('/api/users/profesores')
    profesoresDisponibles.value = res.data
  } catch { /* no bloquea */ }
}

function openAddProfesor() {
  showAddProfesor.value = true
  if (!profesoresDisponibles.value.length) loadProfesores()
}

const profesoresDisponiblesFiltrados = computed(() => {
  const asignados = new Set((asignatura.value?.profesores ?? []).map(p => p.idProfesor))
  return profesoresDisponibles.value.filter(p => !asignados.has(p.idUsuario))
})

async function confirmAddProfesor() {
  if (selectedProfesorId.value === '' || !asignatura.value) return
  addingProfesor.value = true
  try {
    await addProfesorToSubject(asignatura.value.idAsignatura, Number(selectedProfesorId.value))
    const p = profesoresDisponibles.value.find(x => x.idUsuario === selectedProfesorId.value)
    if (p) {
      asignatura.value.profesores = [
        ...(asignatura.value.profesores ?? []),
        { idProfesor: p.idUsuario, nombreProfesor: p.nombreCompleto, fotoPerfil: null }
      ]
    }
    showAddProfesor.value = false
    selectedProfesorId.value = ''
  } finally {
    addingProfesor.value = false
  }
}

async function removeProfesor(idProfesor: number, _nombre: string) {
  if (!asignatura.value) return
  await removeProfesorFromSubject(asignatura.value.idAsignatura, idProfesor)
  asignatura.value.profesores = (asignatura.value.profesores ?? []).filter(p => p.idProfesor !== idProfesor)
}

// selectedTemaId persists across load() because we store the ID, not the object
const selectedTemaId = ref<number | null>(null)
const selectedTema = computed(() =>
  asignatura.value?.temas?.find(t => t.idTema === selectedTemaId.value) ?? null
)

async function load() {
  try {
    const res = await getSubject(Number(route.params.id))
    const detalle = (res.data as any).detalle ?? res.data
    asignatura.value = detalle
    // Abrir el primer tema por defecto
    if (detalle?.temas?.length) {
      openTemaId.value = detalle.temas[0].idTema!
    }
  } finally {
    loading.value = false
  }
}

onMounted(load)

function toggleTema(id: number) {
  if (openTemaId.value === id) {
    // Closing
    openTemaId.value = null
    selectedTemaId.value = null
    if (selectedContent.value?.tema.idTema === id) selectedContent.value = null
  } else {
    // Opening: select this tema, clear any content from a different tema
    openTemaId.value = id
    selectedTemaId.value = id
    if (selectedContent.value?.tema.idTema !== id) selectedContent.value = null
  }
}

function selectContent(tema: TemaVM, contenido: ContenidoVM) {
  selectedContent.value = { tema, contenido }
  openTemaId.value = tema.idTema!
  selectedTemaId.value = tema.idTema!
  mobileSidebarOpen.value = false
}

function contentIcon(tipo: string) {
  const icons: Record<string, string> = {
    Video: 'fa-video', Documento: 'fa-file-alt', Enlace: 'fa-link',
    Tarea: 'fa-laptop-code', Quiz: 'fa-question-circle', Examen: 'fa-circle-question',
  }
  return `fas ${icons[tipo] ?? 'fa-book'}`
}

function contentTypeColor(tipo: string) {
  const colors: Record<string, string> = {
    Video: 'type-video', Documento: 'type-doc', Enlace: 'type-link',
    Tarea: 'type-task', Quiz: 'type-task', Examen: 'type-task',
  }
  return colors[tipo] ?? 'type-default'
}

function contentRoute(idContenido: number, tipo: string) {
  const map: Record<string, string> = {
    Video: 'video', Documento: 'documento', Enlace: 'enlace', Tarea: 'tarea', Examen: 'examen',
  }
  return `/contenido/${idContenido}/${map[tipo] ?? 'documento'}`
}


async function confirmDeleteTema(idTema: number, nombre: string) {
  const result = await Swal.fire({
    title: `¿Eliminar módulo "${nombre}"?`,
    text: 'Se eliminarán todos los contenidos del módulo. Esta acción no se puede deshacer.',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Sí, eliminar',
    cancelButtonText: 'Cancelar',
    confirmButtonColor: '#dc3545',
  })
  if (!result.isConfirmed) return
  await deleteTopic(idTema)
  if (selectedContent.value?.tema.idTema === idTema) selectedContent.value = null
  if (openTemaId.value === idTema) openTemaId.value = null
  if (selectedTemaId.value === idTema) selectedTemaId.value = null
  await load()
}

async function confirmDeleteContenido(idContenido: number, titulo: string) {
  const result = await Swal.fire({
    title: `¿Eliminar "${titulo}"?`,
    text: 'Se eliminarán las entregas y calificaciones asociadas. Esta acción no se puede deshacer.',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonText: 'Sí, eliminar',
    cancelButtonText: 'Cancelar',
    confirmButtonColor: '#dc3545',
  })
  if (!result.isConfirmed) return
  await deleteContent(idContenido)
  if (selectedContent.value?.contenido.idContenido === idContenido) selectedContent.value = null
  await load()
}

async function saveTopic() {
  if (!asignatura.value) return
  savingTopic.value = true
  try {
    await createTopic({ ...newTopic.value, idAsignatura: asignatura.value.idAsignatura })
    showTopicModal.value = false
    newTopic.value = { nombre: '', orden: 1 }
    await load()
  } finally {
    savingTopic.value = false
  }
}
</script>

<template>
  <div class="page">
    <div v-if="loading" class="loading-center"><div class="spinner"></div></div>

    <div v-else-if="asignatura" class="two-panel">

      <!-- Mobile: botón para mostrar/ocultar sidebar -->
      <div class="mobile-sidebar-toggle">
        <button class="mobile-toggle-btn" @click="mobileSidebarOpen = !mobileSidebarOpen">
          <i class="fas" :class="mobileSidebarOpen ? 'fa-times' : 'fa-bars'"></i>
          {{ mobileSidebarOpen ? 'Cerrar módulos' : 'Ver módulos' }}
        </button>
      </div>

      <!-- ===== SIDEBAR ===== -->
      <aside class="sidebar" :class="{ 'sidebar-mobile-open': mobileSidebarOpen }">
        <div class="sidebar-header">
          <!-- Nombre editable -->
          <div v-if="!editingName" class="sidebar-subject-name-wrap">
            <span class="sidebar-subject-name">{{ asignatura.nombreAsignatura }}</span>
            <button v-if="canEdit" class="edit-name-btn" title="Editar nombre" @click="startEditName">
              <i class="fas fa-pencil-alt"></i>
            </button>
          </div>
          <div v-else class="sidebar-name-edit">
            <input
              v-model="editingNameValue"
              class="form-control form-control-sm"
              @keydown.enter.prevent="saveEditName"
              @keydown.escape="editingName = false"
              autofocus
            />
            <div class="name-edit-actions">
              <button class="btn btn-primary btn-xs" :disabled="savingName" @click="saveEditName">
                <i v-if="savingName" class="fas fa-spinner fa-spin"></i>
                <i v-else class="fas fa-check"></i>
              </button>
              <button class="btn btn-outline-secondary btn-xs" @click="editingName = false">
                <i class="fas fa-times"></i>
              </button>
            </div>
          </div>

          <!-- Profesores -->
          <div class="sidebar-teachers">
            <template v-if="(asignatura.profesores ?? []).length > 0">
              <span v-for="p in asignatura.profesores" :key="p.idProfesor" class="teacher-pill">
                <div class="teacher-av">
                  <img v-if="p.fotoPerfil" :src="`/assets/images/users/${p.fotoPerfil}`" :alt="p.nombreProfesor" />
                  <span v-else>{{ p.nombreProfesor[0] }}</span>
                </div>
                {{ p.nombreProfesor }}
                <button v-if="canEdit" class="remove-profesor-btn" title="Desasignar profesor" @click="removeProfesor(p.idProfesor, p.nombreProfesor)">
                  <i class="fas fa-times"></i>
                </button>
              </span>
            </template>
            <span v-else-if="!canEdit" class="no-profesor-text">Sin profesor asignado</span>
          </div>

          <!-- Añadir profesor (solo canEdit) -->
          <div v-if="canEdit" class="add-profesor-area">
            <div v-if="showAddProfesor" class="add-profesor-form">
              <select v-model="selectedProfesorId" class="form-control form-control-sm">
                <option value="">— Selecciona un profesor —</option>
                <option v-for="p in profesoresDisponiblesFiltrados" :key="p.idUsuario" :value="p.idUsuario">
                  {{ p.nombreCompleto }}
                </option>
              </select>
              <div class="name-edit-actions">
                <button class="btn btn-primary btn-xs" :disabled="addingProfesor || selectedProfesorId === ''" @click="confirmAddProfesor">
                  <i v-if="addingProfesor" class="fas fa-spinner fa-spin"></i>
                  <i v-else class="fas fa-check"></i>
                </button>
                <button class="btn btn-outline-secondary btn-xs" @click="showAddProfesor = false; selectedProfesorId = ''">
                  <i class="fas fa-times"></i>
                </button>
              </div>
            </div>
            <button v-else class="add-profesor-btn" @click="openAddProfesor">
              <i class="fas fa-plus"></i> Añadir profesor
            </button>
          </div>
        </div>

        <nav class="sidebar-nav">
          <div v-for="tema in asignatura.temas" :key="tema.idTema" class="tema-group">
            <!-- Tema header — acordeón exclusivo -->
            <div class="tema-toggle-wrap">
              <button
                class="tema-toggle"
                :class="{ 'tema-toggle-open': openTemaId === tema.idTema }"
                @click="toggleTema(tema.idTema!)"
              >
                <span class="tema-name">{{ tema.nombreTema }}</span>
                <i class="fas" :class="openTemaId === tema.idTema ? 'fa-chevron-up' : 'fa-chevron-down'" />
              </button>
              <button
                v-if="canEdit"
                class="trash-btn"
                title="Eliminar módulo"
                @click.stop="confirmDeleteTema(tema.idTema!, tema.nombreTema)"
              ><i class="fas fa-trash"></i></button>
            </div>

            <!-- Contenidos del tema (colapsable) -->
            <div class="tema-items" :class="{ open: openTemaId === tema.idTema }">
              <div
                v-for="c in tema.contenidos ?? []"
                :key="c.idContenido"
                class="sidebar-item-wrap"
              >
                <button
                  class="sidebar-item"
                  :class="{ 'sidebar-item-active': selectedContent?.contenido.idContenido === c.idContenido }"
                  @click="selectContent(tema, c)"
                >
                  <i :class="contentIcon(c.tipoContenido)" class="sidebar-item-icon"></i>
                  <span class="sidebar-item-text">{{ c.tituloContenido }}</span>
                  <i v-if="c.contenido_Completado" class="fas fa-check-circle sidebar-item-check"></i>
                </button>
                <button
                  v-if="canEdit"
                  class="trash-btn trash-btn-sm"
                  title="Eliminar contenido"
                  @click.stop="confirmDeleteContenido(c.idContenido!, c.tituloContenido)"
                ><i class="fas fa-trash"></i></button>
              </div>
              <div v-if="!tema.contenidos?.length" class="sidebar-empty">Sin contenidos aún</div>
            </div>
          </div>
        </nav>

        <div v-if="canEdit" class="sidebar-footer">
          <button class="add-topic-btn" @click="showTopicModal = true">
            <i class="fas fa-plus"></i> Añadir módulo
          </button>
        </div>
      </aside>

      <!-- ===== PANEL DERECHO ===== -->
      <main class="main-panel">

        <!-- Estado 1: sin módulos -->
        <div v-if="!hasTemas" class="empty-selection">
          <div class="empty-selection-icon"><i class="fas fa-layer-group"></i></div>
          <h3 class="empty-selection-title">Esta asignatura aún no tiene módulos</h3>
          <p class="empty-selection-desc">Crea el primer módulo para empezar a añadir material.</p>
          <button v-if="canEdit" class="btn btn-primary btn-add-first" @click="showTopicModal = true">
            <i class="fas fa-plus"></i> Añadir primer módulo
          </button>
        </div>

        <!-- Estado 2: contenido seleccionado -->
        <div v-else-if="selectedContent" class="content-detail">
          <div class="content-breadcrumb">
            <span class="bc-tema">{{ selectedContent.tema.nombreTema }}</span>
            <i class="fas fa-chevron-right bc-sep"></i>
            <span class="bc-current">{{ selectedContent.contenido.tituloContenido }}</span>
          </div>

          <div class="content-card">
            <div class="content-meta-row">
              <div class="content-type-badge" :class="contentTypeColor(selectedContent.contenido.tipoContenido)">
                <i :class="contentIcon(selectedContent.contenido.tipoContenido)"></i>
                <span>{{ selectedContent.contenido.tipoContenido }}</span>
              </div>
              <span class="content-status-pill"
                :class="selectedContent.contenido.contenido_Completado ? 'status-done' : 'status-pending'">
                <i :class="selectedContent.contenido.contenido_Completado ? 'fas fa-check-circle' : 'fas fa-clock'"></i>
                {{ selectedContent.contenido.contenido_Completado ? 'Completado' : 'Pendiente' }}
              </span>
            </div>
            <h1 class="content-detail-title">{{ selectedContent.contenido.tituloContenido }}</h1>
            <p v-if="selectedContent.contenido.descripcionContenido" class="content-detail-desc">
              {{ selectedContent.contenido.descripcionContenido }}
            </p>
          </div>

          <div class="content-action">
            <RouterLink
              :to="contentRoute(selectedContent.contenido.idContenido!, selectedContent.contenido.tipoContenido)"
              class="btn btn-primary btn-lg"
            >
              <i :class="contentIcon(selectedContent.contenido.tipoContenido)"></i>
              <span v-if="selectedContent.contenido.tipoContenido === 'Video'">Ver vídeo</span>
              <span v-else-if="selectedContent.contenido.tipoContenido === 'Documento'">Ver documento</span>
              <span v-else-if="selectedContent.contenido.tipoContenido === 'Enlace'">Visitar enlace</span>
              <span v-else-if="selectedContent.contenido.tipoContenido === 'Tarea'">Ver tarea</span>
              <span v-else-if="selectedContent.contenido.tipoContenido === 'Examen'">Ir al examen</span>
              <span v-else>Abrir contenido</span>
            </RouterLink>
          </div>

          <div v-if="canEdit" class="add-content-area">
            <div v-if="addingContentFor === selectedContent.tema.idTema">
              <ContentForm
                :id-tema="selectedContent.tema.idTema!"
                :id-asignatura="asignatura.idAsignatura"
                @saved="() => { addingContentFor = null; load() }"
                @cancel="addingContentFor = null"
              />
            </div>
            <button v-else class="add-content-btn" @click="addingContentFor = selectedContent!.tema.idTema!">
              <i class="fas fa-plus"></i> Añadir contenido a este módulo
            </button>
          </div>
        </div>

        <!-- Estado 3: módulo seleccionado sin contenido -->
        <div v-else-if="selectedTema" class="tema-detail">
          <div class="content-breadcrumb">
            <span class="bc-tema">{{ asignatura.nombreAsignatura }}</span>
            <i class="fas fa-chevron-right bc-sep"></i>
            <span class="bc-current">{{ selectedTema.nombreTema }}</span>
          </div>

          <div class="content-card tema-overview-card">
            <div class="tema-overview-icon"><i class="fas fa-layer-group"></i></div>
            <h2 class="tema-overview-title">{{ selectedTema.nombreTema }}</h2>
            <p v-if="!selectedTema.contenidos?.length" class="tema-overview-empty">
              Este módulo no tiene contenidos aún.
            </p>
            <p v-else class="tema-overview-count">
              {{ selectedTema.contenidos.length }} contenido{{ selectedTema.contenidos.length !== 1 ? 's' : '' }} en este módulo.
              Haz clic en uno del menú izquierdo para verlo.
            </p>
          </div>

          <div v-if="canEdit" class="tema-add-content-wrap">
            <div v-if="addingContentFor === selectedTema.idTema">
              <ContentForm
                :id-tema="selectedTema.idTema!"
                :id-asignatura="asignatura.idAsignatura"
                @saved="() => { addingContentFor = null; load() }"
                @cancel="addingContentFor = null"
              />
            </div>
            <button v-else class="btn btn-primary btn-add-content-primary" @click="addingContentFor = selectedTema!.idTema!">
              <i class="fas fa-plus"></i> Añadir contenido a este módulo
            </button>
          </div>
        </div>

        <!-- Estado 4: nada seleccionado -->
        <div v-else class="empty-selection">
          <div class="empty-selection-icon"><i class="fas fa-hand-pointer"></i></div>
          <h3 class="empty-selection-title">Selecciona un módulo</h3>
          <p class="empty-selection-desc">Haz clic en el título de un módulo del menú izquierdo para empezar.</p>
        </div>


      </main>
    </div>

    <div v-else class="loading-center">
      <p style="color: var(--color-muted)">No se pudo cargar la asignatura.</p>
    </div>
  </div>

  <!-- Modal añadir tema -->
  <Teleport to="body">
    <div v-if="showTopicModal" class="modal-overlay" @mousedown.self="showTopicModal = false">
      <div class="modal-dialog">
        <div class="modal-header">
          <h5 class="modal-title">Añadir Nuevo Módulo</h5>
          <button class="modal-close" @click="showTopicModal = false"><i class="fas fa-times"></i></button>
        </div>
        <div class="modal-body">
          <div class="field">
            <label class="form-label">Nombre del módulo <span class="req">*</span></label>
            <input v-model="newTopic.nombre" type="text" class="form-control" required />
          </div>
          <div class="field">
            <label class="form-label">Orden</label>
            <input v-model.number="newTopic.orden" type="number" class="form-control" min="1" />
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-outline-secondary" @click="showTopicModal = false">Cancelar</button>
          <button class="btn btn-primary" @click="saveTopic" :disabled="savingTopic">
            <span v-if="savingTopic"><i class="fas fa-spinner fa-spin"></i> Guardando…</span>
            <span v-else><i class="fas fa-save"></i> Guardar</span>
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.two-panel {
  display: flex;
  max-width: var(--container-max);
  margin: 0 auto;
  min-height: calc(100vh - var(--navbar-height));
  align-items: flex-start;
}

/* ===== SIDEBAR ===== */
.sidebar {
  width: 300px;
  flex-shrink: 0;
  background: var(--color-surface);
  border-right: 1px solid var(--color-border);
  min-height: calc(100vh - var(--navbar-height));
  display: flex;
  flex-direction: column;
  position: sticky;
  top: var(--navbar-height);
  overflow-y: auto;
  max-height: calc(100vh - var(--navbar-height));
}

.sidebar-header {
  padding: var(--sp-5);
  border-bottom: 1px solid var(--color-border);
  background: linear-gradient(135deg, #e8f4fd, #dbeafe);
}
.sidebar-subject-name-wrap {
  display: flex;
  align-items: flex-start;
  gap: var(--sp-2);
  margin-bottom: var(--sp-3);
}
.sidebar-subject-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  line-height: 1.4;
  flex: 1;
}
.edit-name-btn {
  background: none;
  border: none;
  padding: 2px 4px;
  cursor: pointer;
  color: var(--color-muted);
  font-size: 11px;
  flex-shrink: 0;
  border-radius: var(--radius-sm);
  transition: color 0.15s, background 0.15s;
}
.edit-name-btn:hover { color: var(--color-primary); background: rgba(0,0,0,0.05); }

.sidebar-name-edit {
  display: flex;
  flex-direction: column;
  gap: var(--sp-2);
  margin-bottom: var(--sp-3);
}
.name-edit-actions { display: flex; gap: var(--sp-1); }
.btn-xs {
  padding: 2px 8px;
  font-size: var(--font-size-xs);
  line-height: 1.5;
  border-radius: var(--radius-sm);
}

.sidebar-teachers { display: flex; flex-wrap: wrap; gap: var(--sp-2); margin-bottom: var(--sp-2); }
.no-profesor-text { font-size: var(--font-size-xs); color: var(--color-muted); font-style: italic; }

.remove-profesor-btn {
  background: none;
  border: none;
  padding: 0 2px;
  cursor: pointer;
  color: var(--color-muted);
  font-size: 9px;
  line-height: 1;
  border-radius: 50%;
  transition: color 0.15s;
  margin-left: 2px;
}
.remove-profesor-btn:hover { color: var(--color-danger); }

.add-profesor-area { margin-top: var(--sp-1); }
.add-profesor-btn {
  background: none;
  border: none;
  padding: 0;
  font-size: var(--font-size-xs);
  color: var(--color-primary);
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 4px;
  font-family: var(--font-sans);
}
.add-profesor-btn:hover { text-decoration: underline; }
.add-profesor-form { display: flex; flex-direction: column; gap: var(--sp-2); }
.teacher-pill {
  display: inline-flex;
  align-items: center;
  gap: var(--sp-1);
  background: white;
  border-radius: 999px;
  padding: 2px var(--sp-2);
  font-size: var(--font-size-xs);
  color: var(--color-text);
  border: 1px solid var(--color-border);
}
.teacher-av {
  width: 20px; height: 20px;
  border-radius: 50%;
  background: var(--color-primary);
  color: white;
  display: flex; align-items: center; justify-content: center;
  font-size: 9px;
  overflow: hidden;
  flex-shrink: 0;
}
.teacher-av img { width: 100%; height: 100%; object-fit: cover; }

.sidebar-nav { flex: 1; overflow-y: auto; }

.tema-group { border-bottom: 1px solid var(--color-border); }

.tema-toggle {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--sp-3) var(--sp-4);
  background: none;
  border: none;
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  cursor: pointer;
  text-align: left;
  transition: background 0.15s;
}
.tema-toggle-wrap {
  display: flex;
  align-items: stretch;
  border-bottom: 1px solid var(--color-border);
}
.tema-toggle-wrap:last-child { border-bottom: none; }
.tema-toggle {
  flex: 1;
  border-bottom: none !important;
}
.tema-toggle:hover { background: var(--color-muted-bg); }
.tema-toggle-open { background: #f0f7ff; color: var(--color-primary); }
.tema-toggle-open:hover { background: #e8f2ff; }
.tema-name { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; min-width: 0; }
.tema-toggle .fas { color: var(--color-muted); font-size: 10px; flex-shrink: 0; }
.tema-toggle-open .fas { color: var(--color-primary); }

.trash-btn {
  flex-shrink: 0;
  background: none;
  border: none;
  border-left: 1px solid var(--color-border);
  padding: 0 var(--sp-3);
  color: var(--color-muted);
  cursor: pointer;
  font-size: 0.75rem;
  line-height: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 2rem;
  opacity: 0;
  transition: opacity 0.15s, background 0.15s, color 0.15s;
}
.tema-toggle-wrap:hover .trash-btn,
.sidebar-item-wrap:hover .trash-btn { opacity: 1; }
/* Papelera siempre visible en el ítem activo */
.sidebar-item-wrap:has(.sidebar-item-active) .trash-btn { opacity: 1; }
.trash-btn:hover { background: #fef2f2; color: var(--color-danger); }
.trash-btn-sm { width: 1.75rem; font-size: 0.75rem; flex-shrink: 0; }

.sidebar-item-wrap {
  display: flex;
  align-items: stretch;
  border-bottom: 1px solid var(--color-border);
}
.sidebar-item-wrap:last-child { border-bottom: none; }
.sidebar-item-wrap .sidebar-item {
  flex: 1;
  border-bottom: none !important;
}

.tema-items { max-height: 0; overflow: hidden; transition: max-height 0.3s ease; }
.tema-items.open { max-height: 800px; }

.sidebar-item {
  display: flex;
  align-items: center;
  gap: var(--sp-2);
  min-width: 0;
  padding: var(--sp-2) var(--sp-4) var(--sp-2) var(--sp-8);
  font-size: var(--font-size-sm);
  color: var(--color-muted);
  background: none;
  border: none;
  border-bottom: 1px solid var(--color-border);
  cursor: pointer;
  text-align: left;
  font-family: var(--font-sans);
  transition: background 0.15s, color 0.15s;
  overflow: hidden;
}
.sidebar-item:last-child { border-bottom: none; }
.sidebar-item:hover { background: var(--color-muted-bg); color: var(--color-text); }
.sidebar-item-active {
  background: var(--color-primary);
  color: white;
  font-weight: var(--font-weight-medium);
}
.sidebar-item-active:hover { background: #0b5ed7; color: white; }
.sidebar-item-icon { width: 14px; text-align: center; flex-shrink: 0; }
.sidebar-item-text { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.sidebar-item-check { font-size: 11px; flex-shrink: 0; }
.sidebar-item-active .sidebar-item-check { color: rgba(255,255,255,0.8); }
.sidebar-item:not(.sidebar-item-active) .sidebar-item-check { color: #16a34a; }

.sidebar-empty {
  padding: var(--sp-2) var(--sp-4) var(--sp-2) var(--sp-8);
  font-size: var(--font-size-xs);
  color: var(--color-muted);
  font-style: italic;
}

.sidebar-footer {
  padding: var(--sp-4);
  border-top: 1px solid var(--color-border);
}
.add-topic-btn {
  display: flex;
  align-items: center;
  gap: var(--sp-2);
  width: 100%;
  padding: var(--sp-2) var(--sp-3);
  background: none;
  border: 1px dashed var(--color-primary);
  border-radius: var(--radius-md);
  color: var(--color-primary);
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  cursor: pointer;
  transition: background 0.15s;
}
.add-topic-btn:hover { background: color-mix(in srgb, var(--color-primary) 8%, transparent); }

/* ===== PANEL DERECHO ===== */
.main-panel {
  flex: 1;
  padding: 2rem 2.5rem;
  min-width: 0;
}

/* Empty state */
.empty-selection {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: var(--sp-16) var(--sp-4);
  color: var(--color-muted);
  min-height: 400px;
}
.empty-selection-icon {
  font-size: 3rem;
  margin-bottom: var(--sp-5);
  opacity: 0.4;
}
.empty-selection-title {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0 0 var(--sp-3);
}
.empty-selection-desc {
  font-size: var(--font-size-sm);
  margin: 0 0 var(--sp-5);
  max-width: 300px;
}
.btn-add-first {
  display: inline-flex;
  align-items: center;
  gap: var(--sp-2);
}

/* Content detail */
.content-detail {
  max-width: 680px;
}

.content-breadcrumb {
  display: flex;
  align-items: center;
  gap: var(--sp-2);
  font-size: var(--font-size-sm);
  color: var(--color-muted);
  margin-bottom: 1.25rem;
}
.bc-tema { color: var(--color-muted); }
.bc-sep { font-size: 10px; }
.bc-current { color: var(--color-text); font-weight: var(--font-weight-medium); }

/* Card principal del contenido */
.content-card {
  background: var(--color-surface);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  padding: 1.75rem 2rem;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.08);
  margin-bottom: 1.5rem;
}

.content-meta-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: var(--sp-3);
  margin-bottom: 1rem;
}

.content-type-badge {
  display: inline-flex;
  align-items: center;
  gap: var(--sp-2);
  border-radius: 999px;
  padding: 4px 12px;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-semibold);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}
.type-video { background: #fee2e2; color: #dc2626; }
.type-doc { background: #dbeafe; color: #1d4ed8; }
.type-link { background: #dcfce7; color: #16a34a; }
.type-task { background: #fef9c3; color: #a16207; }
.type-default { background: var(--color-muted-bg); color: var(--color-muted); }

.content-status-pill {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  padding: 4px 12px;
  border-radius: 999px;
  font-size: var(--font-size-xs);
  font-weight: var(--font-weight-medium);
  white-space: nowrap;
}
.status-done { background: #d1fae5; color: #065f46; }
.status-pending { background: #fef3c7; color: #92400e; }

.content-detail-title {
  font-size: 1.5rem;
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  margin: 0 0 0.875rem;
  line-height: 1.3;
}

.content-detail-desc {
  font-size: var(--font-size-sm);
  color: var(--color-muted);
  line-height: 1.75;
  margin: 0;
}

/* Botón de acción */
.content-action {
  margin-bottom: 2rem;
}
.btn-lg {
  padding: var(--sp-3) var(--sp-6);
  font-size: var(--font-size-md);
  display: inline-flex;
  align-items: center;
  gap: var(--sp-2);
}

/* Tema detail (estado 3) */
.tema-detail { max-width: 680px; }
.tema-overview-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: var(--sp-8) var(--sp-6);
  margin-bottom: 1.5rem;
}
.tema-overview-icon {
  font-size: 2.5rem;
  color: var(--color-primary);
  opacity: 0.5;
  margin-bottom: var(--sp-4);
}
.tema-overview-title {
  font-size: var(--font-size-xl);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  margin: 0 0 var(--sp-3);
}
.tema-overview-empty {
  font-size: var(--font-size-sm);
  color: var(--color-muted);
  margin: 0;
}
.tema-overview-count {
  font-size: var(--font-size-sm);
  color: var(--color-muted);
  margin: 0;
}
.tema-add-content-wrap {
  display: flex;
  justify-content: center;
}
.btn-add-content-primary {
  display: inline-flex;
  align-items: center;
  gap: var(--sp-2);
  padding: var(--sp-3) var(--sp-6);
  font-size: var(--font-size-md);
}

/* Zona añadir contenido */
.add-content-area {
  margin-top: 2rem;
  padding-top: 1.5rem;
  border-top: 1px dashed var(--color-border);
}
.add-content-btn {
  display: inline-flex;
  align-items: center;
  gap: var(--sp-2);
  padding: var(--sp-2) var(--sp-4);
  background: none;
  border: 1px dashed var(--color-primary);
  border-radius: var(--radius-md);
  color: var(--color-primary);
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: background 0.15s;
}
.add-content-btn:hover { background: color-mix(in srgb, var(--color-primary) 8%, transparent); }

.add-content-floating { margin-top: var(--sp-8); text-align: center; }
.add-hint { font-size: var(--font-size-sm); color: var(--color-muted); }

/* Modal */
.modal-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: var(--sp-5) var(--sp-6);
  border-bottom: 1px solid var(--color-border);
}
.modal-title { font-size: var(--font-size-lg); font-weight: var(--font-weight-semibold); margin: 0; color: var(--color-text); }
.modal-close { background: none; border: none; color: var(--color-muted); font-size: var(--font-size-md); cursor: pointer; padding: var(--sp-1); transition: color 0.15s; }
.modal-close:hover { color: var(--color-text); }
.modal-body { padding: var(--sp-5) var(--sp-6); display: flex; flex-direction: column; gap: var(--sp-4); }
.modal-footer { display: flex; justify-content: flex-end; gap: var(--sp-3); padding: var(--sp-4) var(--sp-6); border-top: 1px solid var(--color-border); }
.field { display: flex; flex-direction: column; gap: var(--sp-1); }
.req { color: var(--color-danger); }

.loading-center { display: flex; justify-content: center; align-items: center; padding: var(--sp-12) 0; }

/* ===== MOBILE SIDEBAR TOGGLE ===== */
.mobile-sidebar-toggle { display: none; }

.mobile-toggle-btn {
  display: flex;
  align-items: center;
  gap: var(--sp-2);
  width: 100%;
  padding: var(--sp-3) var(--sp-4);
  background: var(--color-surface);
  border: none;
  border-bottom: 1px solid var(--color-border);
  font-family: var(--font-sans);
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-medium);
  color: var(--color-primary);
  cursor: pointer;
}

@media (max-width: 768px) {
  .mobile-sidebar-toggle { display: block; }

  .two-panel { flex-direction: column; }

  .sidebar {
    width: 100%;
    position: static;
    max-height: none;
    min-height: auto;
    border-right: none;
    border-bottom: 1px solid var(--color-border);
    /* oculta por defecto en mobile */
    display: none;
  }
  .sidebar.sidebar-mobile-open { display: flex; }

  .main-panel { padding: var(--sp-4); }
  .content-detail { max-width: 100%; }
}
</style>
