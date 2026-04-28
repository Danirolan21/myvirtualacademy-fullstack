<script setup lang="ts">
import { useAuthStore } from '../stores/auth'
const auth = useAuthStore()
</script>

<template>
  <div class="home">

    <!-- ── Hero ── -->
    <section class="hero">
      <div class="hero-overlay"></div>
      <div class="hero-content">
        <h1 class="hero-heading">BIENVENIDO A TU <br />CAMPUS VIRTUAL</h1>
        <p class="hero-sub">
          Tu viaje de aprendizaje continúa aquí. Accede a tus cursos, sigue tu progreso,
          conéctate con compañeros y destaca en tu camino educativo.
        </p>
      </div>
    </section>

    <!-- ── Features ── -->
    <section class="features-section">
      <div class="container">
        <h2 class="features-heading">Descubre lo que Ofrece Nuestra Plataforma</h2>
        <div class="features-grid">
          <div class="feature-card">
            <div class="feature-icon"><i class="fas fa-laptop"></i></div>
            <h3 class="feature-title">Aprendizaje Interactivo</h3>
            <p class="feature-desc">Interactúa con contenido dinámico, simulaciones y ejercicios diseñados para mejorar tu comprensión y retención.</p>
          </div>
          <div class="feature-card">
            <div class="feature-icon"><i class="fas fa-users"></i></div>
            <h3 class="feature-title">Espacios Colaborativos</h3>
            <p class="feature-desc">Conéctate con compañeros e instructores a través de foros de discusión, proyectos en grupo y salas de estudio virtuales.</p>
          </div>
          <div class="feature-card">
            <div class="feature-icon"><i class="fas fa-chart-line"></i></div>
            <h3 class="feature-title">Seguimiento del Progreso</h3>
            <p class="feature-desc">Monitorea tu trayectoria académica con análisis detallados, información sobre tu rendimiento y retroalimentación personalizada.</p>
          </div>
          <div class="feature-card">
            <div class="feature-icon"><i class="fas fa-medal"></i></div>
            <h3 class="feature-title">Certificaciones</h3>
            <p class="feature-desc">Obtén certificados reconocidos al completar cursos para demostrar tus logros y fortalecer tu perfil profesional.</p>
          </div>
        </div>
      </div>
    </section>

    <!-- ── CTA ── -->
    <div class="container">
      <section class="cta-section">
        <h2 class="cta-title">¿Listo para Potenciar tu Aprendizaje?</h2>
        <p class="cta-desc">
          Explora nuestra amplia biblioteca de cursos y comienza a mejorar tus habilidades hoy.
          Nuestros instructores están listos para guiarte en tu viaje educativo.
        </p>
        <RouterLink
          v-if="!auth.isAuthenticated"
          to="/login"
          class="cta-btn"
        >Explorar Cursos</RouterLink>
        <RouterLink
          v-else
          :to="auth.isAdmin ? '/admin' : auth.role === 'Estudiante' ? '/estudiante' : '/profesor'"
          class="cta-btn"
        >Ir a mi área</RouterLink>
      </section>
    </div>

  </div>
</template>

<style scoped>
/* ── Hero ── */
.hero {
  position: relative;
  background: #343a40 url('/assets/images/bg-masthead.webp') no-repeat center center;
  background-size: cover;
  padding: 8rem var(--container-padding);
  text-align: center;
}

.hero-overlay {
  position: absolute;
  inset: 0;
  background-color: #1c375e;
  opacity: 0.5;
  z-index: 1;
}

.hero-content {
  position: relative;
  z-index: 2;
  max-width: 800px;
  margin-inline: auto;
}

.hero-heading {
  font-size: clamp(2.5rem, 6vw, 4.5rem);
  font-weight: var(--font-weight-bold);
  color: var(--color-white);
  line-height: 1.15;
  margin: 0 0 var(--sp-5);
  letter-spacing: 0.02em;
}

.hero-sub {
  font-size: var(--font-size-lg);
  color: rgba(255, 255, 255, 0.9);
  max-width: 600px;
  margin-inline: auto;
  line-height: var(--line-height-base);
}

/* ── Features ── */
.features-section {
  padding: var(--sp-12) 0;
  background: var(--color-muted-bg);
  margin: var(--sp-10) 0;
  border-radius: var(--radius-xl);
}

.features-heading {
  text-align: center;
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-semibold);
  color: var(--color-text);
  margin: 0 0 var(--sp-10);
}

.features-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(230px, 1fr));
  gap: var(--sp-6);
  padding: 0 var(--sp-5);
}

.feature-card {
  background: var(--color-surface);
  border-radius: var(--radius-lg);
  padding: var(--sp-8) var(--sp-6);
  text-align: center;
  box-shadow: var(--shadow-md);
  transition: transform 0.25s, box-shadow 0.25s;
}
.feature-card:hover {
  transform: translateY(-5px);
  box-shadow: var(--shadow-hover);
}

.feature-icon {
  background: var(--color-primary);
  color: var(--color-white);
  width: 70px;
  height: 70px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: var(--radius-full);
  margin: 0 auto var(--sp-5);
  font-size: 1.75rem;
}

.feature-title {
  font-size: var(--font-size-lg);
  font-weight: var(--font-weight-semibold);
  color: var(--color-primary);
  margin: 0 0 var(--sp-3);
}

.feature-desc {
  color: var(--color-muted);
  line-height: var(--line-height-base);
  font-size: var(--font-size-sm);
  margin: 0;
}

/* ── CTA ── */
.cta-section {
  background: linear-gradient(135deg, #ff6b6b, #e74c3c);
  padding: var(--sp-12) var(--sp-8);
  text-align: center;
  color: var(--color-white);
  border-radius: var(--radius-xl);
  margin: var(--sp-10) 0;
}

.cta-title {
  font-size: var(--font-size-2xl);
  font-weight: var(--font-weight-bold);
  margin: 0 0 var(--sp-4);
}

.cta-desc {
  max-width: 580px;
  margin: 0 auto var(--sp-8);
  font-size: var(--font-size-md);
  line-height: var(--line-height-base);
  opacity: 0.95;
}

.cta-btn {
  display: inline-block;
  background: var(--color-white);
  color: #e74c3c;
  padding: var(--sp-3) var(--sp-8);
  border-radius: var(--radius-full);
  font-weight: var(--font-weight-semibold);
  font-size: var(--font-size-md);
  text-decoration: none;
  transition: transform 0.2s, box-shadow 0.2s;
}
.cta-btn:hover {
  transform: scale(1.05);
  box-shadow: var(--shadow-hover);
  text-decoration: none;
  color: #e74c3c;
}
</style>
