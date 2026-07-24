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
        <RouterLink class="button" to="/campanhas/nova">Nova campanha</RouterLink>
      </header>
      <RouterView />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();
const title = computed(() => String(route.meta.title || 'Dashboard'));
const subtitle = computed(() => String(route.meta.subtitle || 'Operacao comercial'));
</script>
