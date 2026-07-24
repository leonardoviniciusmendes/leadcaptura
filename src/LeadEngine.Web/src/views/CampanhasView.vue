<template>
  <main class="page">
    <section class="page-header row">
      <div>
        <p class="eyebrow">Campanhas</p>
        <h1>Campanhas</h1>
        <p class="subtitle">Gerencie campanhas geradas, revisadas e publicadas.</p>
      </div>
      <RouterLink class="button" to="/campanhas/nova">Nova campanha</RouterLink>
    </section>

    <section class="metrics-grid">
      <MetricCard label="Total" :value="campanhas.length" />
      <MetricCard label="Revisadas" :value="countStatus('Revisada')" />
      <MetricCard label="Publicadas" :value="campanhas.filter((item) => item.publicada).length" />
      <MetricCard label="Com erro" :value="countStatus('Erro')" />
    </section>

    <p v-if="error" class="error">{{ error }}</p>

    <SkeletonBlock v-if="loading" :count="6" />
    <EmptyState v-else-if="campanhas.length === 0" title="Nenhuma campanha criada" message="Gere a primeira campanha para iniciar o fluxo de revisao e publicacao.">
      <RouterLink class="button" to="/campanhas/nova">Criar campanha</RouterLink>
    </EmptyState>

    <section v-else class="campaign-grid">
      <RouterLink v-for="campanha in campanhas" :key="campanha.id" class="campaign-tile" :to="`/campanhas/${campanha.id}`">
        <header>
          <span class="status" :class="statusClass(campanha.status)">{{ campanha.status }}</span>
          <span v-if="campanha.publicada" class="status status-publicada">Landing ativa</span>
        </header>
        <h2>{{ campanha.nome }}</h2>
        <p>{{ localizacao(campanha) }}</p>
        <dl class="tile-meta">
          <div><dt>Publico</dt><dd>{{ labelPublico(campanha.tipoPublico) }}</dd></div>
          <div><dt>Operadora</dt><dd>{{ campanha.operadora }}</dd></div>
          <div><dt>Orcamento</dt><dd>{{ money(campanha.orcamentoDiario) }}</dd></div>
          <div><dt>Criacao</dt><dd>{{ date(campanha.dataCriacao) }}</dd></div>
        </dl>
      </RouterLink>
    </section>
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import EmptyState from '../components/EmptyState.vue';
import MetricCard from '../components/MetricCard.vue';
import SkeletonBlock from '../components/SkeletonBlock.vue';
import { listarCampanhas, type Campanha, type StatusCampanha, type TipoPublicoCampanha } from '../services/api';

const campanhas = ref<Campanha[]>([]);
const loading = ref(false);
const error = ref('');

onMounted(load);

async function load() {
  loading.value = true;
  error.value = '';
  try {
    campanhas.value = await listarCampanhas();
  } catch {
    error.value = 'Nao foi possivel carregar as campanhas.';
  } finally {
    loading.value = false;
  }
}

function countStatus(status: StatusCampanha) {
  return campanhas.value.filter((campanha) => campanha.status === status).length;
}

function localizacao(campanha: Campanha) {
  return [campanha.regiao, campanha.cidade, campanha.estado].filter(Boolean).join(' / ');
}

function money(value: number) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
}

function date(value: string) {
  return new Intl.DateTimeFormat('pt-BR').format(new Date(value));
}

function labelPublico(value: TipoPublicoCampanha) {
  const labels: Record<TipoPublicoCampanha, string> = {
    Individual: 'Individual',
    Casal: 'Casal',
    Familia: 'Familia',
    Mei: 'MEI',
    Empresa: 'Empresa'
  };
  return labels[value];
}

function statusClass(status: StatusCampanha) {
  return `status-${status.toLowerCase()}`;
}
</script>
