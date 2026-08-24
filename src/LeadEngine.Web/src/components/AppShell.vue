<template>
  <div class="app-shell">
    <aside class="sidebar">
      <RouterLink class="brand-block" to="/">
        <span class="brand-mark">L</span>
        <span>
          <strong>LeadEngine</strong>
          <small>Health Ads SaaS</small>
        </span>
      </RouterLink>

      <nav class="side-nav">
        <RouterLink to="/">Dashboard</RouterLink>
        <RouterLink to="/campanhas">Campanhas</RouterLink>
        <RouterLink to="/campanhas/nova">Nova campanha</RouterLink>
        <RouterLink to="/leads">Leads</RouterLink>
        <RouterLink to="/googleads/dashboard">Google Ads</RouterLink>
        <RouterLink to="/configuracoes">Configuracoes</RouterLink>
      </nav>
    </aside>

    <div class="workspace">
      <header class="app-header">
        <div>
          <strong>{{ title }}</strong>
          <span>{{ subtitle }}</span>
        </div>
        <div class="actions">
          <RouterLink class="button" to="/campanhas/nova">Nova campanha</RouterLink>
          <button class="button secondary" @click="sair">Sair</button>
        </div>
      </header>
      <RouterView />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { logout } from '../services/api';
import { setCurrentUser } from '../services/auth';

const route = useRoute();
const router = useRouter();
const title = computed(() => String(route.meta.title || 'Dashboard'));
const subtitle = computed(() => String(route.meta.subtitle || 'Operacao comercial'));

async function sair() {
  await logout();
  setCurrentUser(null);
  await router.replace('/login');
}
</script>
