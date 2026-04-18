<script setup lang="ts">
import { useAuthStore } from '../stores/auth'
const auth = useAuthStore()
</script>

<template>
  <section id="dashboard-page">
    <div class="welcome-section">
      <h1 class="welcome-heading">BIENVENIDO A TU <br /> CAMPUS VIRTUAL</h1>
      <p>Tu viaje de aprendizaje continúa aquí. Accede a tus cursos, sigue tu progreso, conéctate con compañeros y destaca en tu camino educativo.</p>
    </div>

    <div class="features-section">
      <div class="container">
        <h2 class="features-heading">Descubre lo que Ofrece Nuestra Plataforma</h2>
        <div class="features-grid">
          <div class="feature-card">
            <div class="feature-icon"><i class="fas fa-laptop"></i></div>
            <h3 class="feature-title">Aprendizaje Interactivo</h3>
            <p class="feature-description">Interactúa con contenido dinámico, simulaciones y ejercicios diseñados para mejorar tu comprensión y retención.</p>
          </div>
          <div class="feature-card">
            <div class="feature-icon"><i class="fas fa-users"></i></div>
            <h3 class="feature-title">Espacios Colaborativos</h3>
            <p class="feature-description">Conéctate con compañeros e instructores a través de foros de discusión, proyectos en grupo y salas de estudio virtuales.</p>
          </div>
          <div class="feature-card">
            <div class="feature-icon"><i class="fas fa-chart-line"></i></div>
            <h3 class="feature-title">Seguimiento del Progreso</h3>
            <p class="feature-description">Monitorea tu trayectoria académica con análisis detallados, información sobre tu rendimiento y retroalimentación personalizada.</p>
          </div>
          <div class="feature-card">
            <div class="feature-icon"><i class="fas fa-medal"></i></div>
            <h3 class="feature-title">Certificaciones</h3>
            <p class="feature-description">Obtén certificados reconocidos al completar cursos, para demostrar tus logros y fortalecer tu perfil profesional.</p>
          </div>
        </div>
      </div>
    </div>

    <div class="container">
      <div class="cta-section">
        <h2 class="cta-title">¿Listo para Potenciar tu Aprendizaje?</h2>
        <p class="cta-description">Explora nuestra amplia biblioteca de cursos y comienza a mejorar tus habilidades hoy.</p>
        <RouterLink v-if="!auth.isAuthenticated" to="/login" class="cta-button">Iniciar Sesión</RouterLink>
        <RouterLink v-else :to="auth.isAdmin ? '/admin' : auth.role === 'Estudiante' ? '/estudiante' : '/profesor'" class="cta-button">Ir a mi área</RouterLink>
      </div>
    </div>
  </section>
</template>

<style scoped>
.features-section {
  padding: 50px 0;
  background-color: #e9ecef;
  margin: 40px 0;
  border-radius: 10px;
}
.features-heading { text-align: center; margin-bottom: 40px; }
.features-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 30px;
  padding: 0 20px;
}
.feature-card {
  background: white;
  border-radius: 8px;
  padding: 25px;
  text-align: center;
  box-shadow: 0 5px 15px rgba(0,0,0,0.05);
  transition: transform 0.3s;
}
.feature-card:hover { transform: translateY(-5px); }
.feature-icon {
  background-color: #4a8bcc;
  color: white;
  width: 70px; height: 70px;
  display: flex; align-items: center; justify-content: center;
  border-radius: 50%;
  margin: 0 auto 20px;
  font-size: 1.8rem;
}
.feature-title { font-size: 1.3rem; font-weight: 600; color: #3a6ea5; margin-bottom: 10px; }
.feature-description { color: #6c757d; }
.cta-section {
  background: linear-gradient(135deg, #ff6b6b, #e74c3c);
  padding: 50px;
  text-align: center;
  color: white;
  border-radius: 10px;
  margin: 40px 0;
}
.cta-title { font-size: 2rem; margin-bottom: 15px; }
.cta-description { max-width: 600px; margin: 0 auto 25px; font-size: 1.1rem; }
.cta-button {
  background: white;
  color: #ff6b6b;
  padding: 12px 30px;
  border-radius: 30px;
  font-weight: 600;
  text-decoration: none;
  transition: transform 0.3s;
}
.cta-button:hover { transform: scale(1.05); }
</style>
