<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '../../stores/auth'
import { getSubject } from '../../api/subjects'
import { createTopic, deleteTopic } from '../../api/topics'
import { deleteContent } from '../../api/content'
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
const newTopic = ref({ nombre: '', orden: 1 })
const savingTopic = ref(false)

const canEdit = ['Profesor', 'Tutor', 'Administrador'].includes(auth.role) || auth.isAdmin

const hasTemas = computed(() => (asignatura.value?.temas?.length ?? 0) > 0)
const hasAnyContent = computed(() =>
  asignatura.value?.temas?.some(t => (t.contenidos?.length ?? 0) > 0) ?? false
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
  // Acordeón exclusivo
  openTemaId.value = openTemaId.value === id ? null : id
  // Al colapsar el tema activo, limpiar selección si el contenido era de ese tema
  if (selectedContent.value?.tema.idTema === id && openTemaId.value === null) {
    selectedContent.value = null
  }
}

function selectContent(tema: TemaVM, contenido: ContenidoVM) {
  selectedContent.value = { tema, contenido }
  // Asegurarse de que el tema esté abierto
  openTemaId.value = tema.idTema!
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

      <!-- ===== SIDEBAR ===== -->
      <aside class="sidebar">
        <div class="sidebar-header">
          <div class="sidebar-subject-name">{{ asignatura.nombreAsignatura }}</div>
          <div class="sidebar-teachers">
            <span v-for="p in asignatura.profesores" :key="p.idProfesor" class="teacher-pill">
              <div class="teacher-av">
                <img v-if="p.fotoPerfil" :src="`/assets/images/users/${p.fotoPerfil}`" :alt="p.nombreProfesor" />
                <span v-else>{{ p.nombreProfesor[0] }}</span>
              </div>
              {{ p.nombreProfesor }}
            </span>
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

        <!-- Empty state: sin módulos en la asignatura -->
        <div v-if="!hasTemas" class="empty-selection">
          <div class="empty-selection-icon"><i class="fas fa-layer-group"></i></div>
          <h3 class="empty-selection-title">Esta asignatura aún no tiene contenidos</h3>
          <p class="empty-selection-desc">Crea el primer módulo para empezar a añadir material.</p>
          <button v-if="canEdit" class="btn btn-primary btn-add-first" @click="showTopicModal = true">
            <i class="fas fa-plus"></i> Añadir primer módulo
          </button>
        </div>

        <!-- Empty state: hay módulos pero ninguno tiene contenidos -->
        <div v-else-if="!hasAnyContent && !selectedContent" class="empty-selection">
          <div class="empty-selection-icon"><i class="fas fa-inbox"></i></div>
          <h3 class="empty-selection-title">Los módulos aún no tienen contenidos</h3>
          <p class="empty-selection-desc">Abre un módulo en la barra izquierda y añade el primer contenido.</p>
        </div>

        <!-- Empty state: hay contenidos pero ninguno seleccionado -->
        <div v-else-if="!selectedContent" class="empty-selection">
          <div class="empty-selection-icon"><i class="fas fa-hand-pointer"></i></div>
          <h3 class="empty-selection-title">Selecciona un contenido</h3>
          <p class="empty-selection-desc">Haz clic en cualquier elemento del menú izquierdo para verlo aquí.</p>
        </div>

        <!-- Contenido seleccionado -->
        <div v-else class="content-detail">
          <!-- Breadcrumb interno -->
          <div class="content-breadcrumb">
            <span class="bc-tema">{{ selectedContent.tema.nombreTema }}</span>
            <i class="fas fa-chevron-right bc-sep"></i>
            <span class="bc-current">{{ selectedContent.contenido.tituloContenido }}</span>
          </div>

          <!-- Card del contenido -->
          <div class="content-card">
            <!-- Fila badge tipo + badge estado -->
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

          <!-- Acción principal según tipo -->
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

          <!-- Sección de añadir contenido (solo editores) -->
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
.sidebar-subject-name {
  font-size: var(--font-size-sm);
  font-weight: var(--font-weight-bold);
  color: var(--color-text);
  margin-bottom: var(--sp-3);
  line-height: 1.4;
}
.sidebar-teachers { display: flex; flex-wrap: wrap; gap: var(--sp-2); }
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
.tema-name { flex: 1; }
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
  font-size: var(--font-size-xs);
  opacity: 0;
  transition: opacity 0.15s, background 0.15s, color 0.15s;
}
.tema-toggle-wrap:hover .trash-btn,
.sidebar-item-wrap:hover .trash-btn { opacity: 1; }
.trash-btn:hover { background: #fef2f2; color: var(--color-danger); }
.trash-btn-sm { padding: 0 var(--sp-2); }

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
  width: 100%;
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

@media (max-width: 768px) {
  .two-panel { flex-direction: column; }
  .sidebar {
    width: 100%;
    position: static;
    max-height: none;
    min-height: auto;
    border-right: none;
    border-bottom: 1px solid var(--color-border);
  }
  .main-panel { padding: var(--sp-4); }
}
</style>
