<template>
  <main class="page dashboard-page">
    <section class="page-header row">
      <div>
        <p class="eyebrow">Overview</p>
        <h1>Dashboard</h1>
        <p class="subtitle">Acompanhe campanhas, publicacoes e leads capturados em um painel unico.</p>
      </div>
      <RouterLink class="button" to="/campanhas/nova">Criar campanha</RouterLink>
    </section>

    <section class="metrics-grid">
      <MetricCard label="Campanhas" :value="campanhas.length" hint="total gerado" />
      <MetricCard label="Revisadas" :value="countStatus('Revisada')" hint="prontas para publicar" />
      <MetricCard label="Publicadas" :value="campanhas.filter((c) => c.publicada).length" hint="landings ativas" />
      <MetricCard label="Leads" :value="leadTotal" hint="capturas registradas" />
    </section>

    <p v-if="error" class="error">{{ error }}</p>

    <section class="dashboard-grid">
      <div class="panel dashboard-panel">
        <header class="section-heading">
          <div>
            <h2>Campanhas recentes</h2>
            <span>Ultimos ativos criados</span>
          </div>
          <RouterLink to="/campanhas">Ver todas</RouterLink>
        </header>

        <SkeletonBlock v-if="loading" :count="5" />
        <EmptyState v-else-if="campanhas.length === 0" title="Nenhuma campanha" message="Crie uma campanha para iniciar a operacao comercial.">
          <RouterLink class="button" to="/campanhas/nova">Nova campanha</RouterLink>
        </EmptyState>
        <div v-else class="card-list">
          <RouterLink v-for="campanha in campanhas.slice(0, 5)" :key="campanha.id" class="campaign-card" :to="`/campanhas/${campanha.id}`">
            <div>
              <strong>{{ campanha.nome }}</strong>
              <span>{{ campanha.cidade }}/{{ campanha.estado }} · {{ campanha.operadora }}</span>
            </div>
            <span class="status" :class="statusClass(campanha.status)">{{ campanha.status }}</span>
          </RouterLink>
        </div>
      </div>

      <div class="panel dashboard-panel">
        <header class="section-heading">
          <div>
            <h2>Leads recentes</h2>
            <span>Capturas da landing page</span>
          </div>
          <RouterLink to="/leads">Abrir leads</RouterLink>
        </header>

        <SkeletonBlock v-if="loading" :count="4" />
        <EmptyState v-else-if="leads.length === 0" title="Sem leads ainda" message="As capturas aparecem aqui quando uma landing publicada receber envios." />
        <div v-else class="card-list compact">
          <article v-for="lead in leads.slice(0, 5)" :key="lead.id" class="lead-card">
            <strong>{{ lead.nome }}</strong>
            <span>{{ lead.campanhaNome || 'Campanha' }} · {{ lead.cidade }}/{{ lead.uf }}</span>
            <small>{{ dateTime(lead.criadoEm) }}</small>
          </article>
        </div>
      </div>
    </section>
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import EmptyState from '../components/EmptyState.vue';
import MetricCard from '../components/MetricCard.vue';
import SkeletonBlock from '../components/SkeletonBlock.vue';
import { listarCampanhas, listarLeads, type Campanha, type Lead, type StatusCampanha } from '../services/api';

const campanhas = ref<Campanha[]>([]);
const leads = ref<Lead[]>([]);
const leadTotal = ref(0);
const loading = ref(false);
const error = ref('');

onMounted(load);

async function load() {
  loading.value = true;
  error.value = '';
  try {
    const [campaignResult, leadResult] = await Promise.all([
      listarCampanhas(),
      listarLeads({ tamanhoPagina: 5 })
    ]);
    campanhas.value = campaignResult;
    leads.value = leadResult.itens;
    leadTotal.value = leadResult.total;
  } catch {
    error.value = 'Nao foi possivel carregar o dashboard.';
  } finally {
    loading.value = false;
  }
}

function countStatus(status: StatusCampanha) {
  return campanhas.value.filter((campanha) => campanha.status === status).length;
}

function statusClass(status: StatusCampanha) {
  return `status-${status.toLowerCase()}`;
}

function dateTime(value: string) {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value));
}
</script>
