import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', component: () => import('../views/HomeView.vue') },
    { path: '/login', component: () => import('../views/auth/LoginView.vue') },
    {
      path: '/register',
      component: () => import('../views/auth/RegisterView.vue'),
      meta: { requiresAuth: true, requiresAdmin: true }
    },
    { path: '/perfil', component: () => import('../views/user/PerfilView.vue'), meta: { requiresAuth: true } },
    { path: '/perfil/editar', redirect: '/perfil' },
    { path: '/admin', component: () => import('../views/admin/AdminView.vue'), meta: { requiresAuth: true, requiresAdmin: true } },
    { path: '/admin/cursos/crear', component: () => import('../views/admin/CreateCourseView.vue'), meta: { requiresAuth: true, requiresAdmin: true } },
    { path: '/admin/cursos/:id', component: () => import('../views/admin/CourseDetailView.vue'), meta: { requiresAuth: true, roles: ['Administrador', 'Profesor', 'Tutor'] } },
    {
      path: '/profesor',
      component: () => import('../views/profesor/ProfesorView.vue'),
      meta: { requiresAuth: true, roles: ['Profesor', 'Tutor', 'Administrador'] }
    },
    { path: '/estudiante', component: () => import('../views/student/EstudianteView.vue'), meta: { requiresAuth: true } },
    { path: '/asignatura/:id', component: () => import('../views/profesor/SubjectDetailView.vue'), meta: { requiresAuth: true } },
    { path: '/contenido/:id/tarea', component: () => import('../views/content/TaskDetailView.vue'), meta: { requiresAuth: true } },
    { path: '/contenido/:id/video', component: () => import('../views/content/VideoDetailView.vue'), meta: { requiresAuth: true } },
    { path: '/contenido/:id/documento', component: () => import('../views/content/DocumentDetailView.vue'), meta: { requiresAuth: true } },
    { path: '/contenido/:id/enlace', component: () => import('../views/content/LinkDetailView.vue'), meta: { requiresAuth: true } },
    { path: '/contenido/:id/examen', component: () => import('../views/content/ExamDetailView.vue'), meta: { requiresAuth: true } },
    { path: '/calendario', component: () => import('../views/CalendarView.vue'), meta: { requiresAuth: true } },
    { path: '/:pathMatch(.*)*', component: () => import('../views/NotFoundView.vue') }
  ]
})

router.beforeEach(async to => {
  const auth = useAuthStore()

  // Intentar restaurar sesión si tenemos token en sessionStorage pero no el user hydratado
  if (auth.accessToken && !auth.user) {
    await auth.silentRefresh()
  }

  if (to.meta.requiresAuth && !auth.isAuthenticated) {
    return { path: '/login', query: { redirect: to.fullPath } }
  }
  if (to.meta.requiresAdmin && !auth.isAdmin) {
    return auth.isAuthenticated ? '/' : { path: '/login', query: { redirect: to.fullPath } }
  }
  if (to.meta.roles) {
    if (!auth.isAuthenticated) return { path: '/login', query: { redirect: to.fullPath } }
    const allowed = to.meta.roles as string[]
    if (!allowed.includes(auth.role ?? '')) return '/'
  }
})

export default router
