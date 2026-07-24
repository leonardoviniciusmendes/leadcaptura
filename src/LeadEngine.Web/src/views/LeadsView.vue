<template>
  <main class="page">
    <section class="page-header row">
      <div>
        <p class="eyebrow">Leads</p>
        <h1>Leads capturados</h1>
        <p class="subtitle">Acompanhe as solicitacoes geradas pelas landings publicas.</p>
      </div>
    </section>

    <section class="metrics-grid">
      <MetricCard label="Total filtrado" :value="leads.length" />
      <MetricCard label="Familia" :value="leads.filter((lead) => lead.tipoContratacao === 'Familiar').length" />
      <MetricCard label="Empresa/MEI" :value="leads.filter((lead) => lead.tipoContratacao === 'Empresarial' || lead.tipoContratacao === 'Mei').length" />
      <MetricCard label="LandingPage" :value="leads.filter((lead) => lead.origem === 'LandingPage').length" />
    </section>

    <form class="panel form-grid compact-filters" @submit.prevent="load">
      <label>Telefone<input v-model.trim="filters.telefone" /></label>
      <label>
        Tipo
        <select v-model="filters.tipoContratacao">
          <option value="">Todos</option>
          <option value="Individual">Individual</option>
          <option value="Familiar">Familiar</option>
          <option value="Empresarial">Empresarial</option>
          <option value="Mei">MEI</option>
          <option value="AindaNaoSei">Ainda nao sei</option>
        </select>
      </label>
      <label>Origem<input v-model.trim="filters.origem" placeholder="LandingPage" /></label>
      <div class="actions"><button class="button" :disabled="loading">Filtrar</button></div>
    </form>

    <p v-if="error" class="error">{{ error }}</p>

    <section class="grid-layout">
      <div class="panel table-panel">
        <SkeletonBlock v-if="loading" :count="6" />
        <EmptyState v-else-if="leads.length === 0" title="Nenhum lead encontrado" message="Os leads capturados pelas landings aparecerao nesta lista." />
        <table v-else>
          <thead>
            <tr>
              <th>Nome</th>
              <th>Telefone</th>
              <th>Campanha</th>
              <th>Cidade/UF</th>
              <th>Vidas</th>
              <th>Tipo</th>
              <th>Data</th>
              <th>Origem</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="lead in leads" :key="lead.id" :class="{ selected: selected?.id === lead.id }" @click="select(lead.id)">
              <td>{{ lead.nome }}</td>
              <td>{{ lead.whatsAppMascarado }}</td>
              <td>{{ lead.campanhaNome || '-' }}</td>
              <td>{{ [lead.cidade, lead.uf].filter(Boolean).join('/') }}</td>
              <td>{{ lead.quantidadeVidas || '-' }}</td>
              <td>{{ lead.tipoContratacao || '-' }}</td>
              <td>{{ dateTime(lead.criadoEm) }}</td>
              <td>{{ lead.origem || '-' }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <aside v-if="selected" class="panel details">
        <p class="eyebrow">Detalhe</p>
        <h2>{{ selected.nome }}</h2>
        <dl>
          <dt>Telefone</dt><dd>{{ selected.whatsAppMascarado }}</dd>
          <dt>E-mail</dt><dd>{{ selected.emailMascarado || '-' }}</dd>
          <dt>Campanha</dt><dd>{{ selected.campanhaNome || '-' }}</dd>
          <dt>Tipo</dt><dd>{{ selected.tipoContratacao || '-' }}</dd>
          <dt>Origem</dt><dd>{{ selected.origemCaptura || selected.origem || '-' }}</dd>
          <dt>Status externo</dt><dd>{{ selected.statusEnvioExterno || '-' }}</dd>
          <dt>UTM campaign</dt><dd>{{ selected.utmCampaign || '-' }}</dd>
        </dl>
      </aside>
    </section>
  </main>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import EmptyState from '../components/EmptyState.vue';
import MetricCard from '../components/MetricCard.vue';
import SkeletonBlock from '../components/SkeletonBlock.vue';
import { listarLeads, obterLead, type Lead } from '../services/api';

const leads = ref<Lead[]>([]);
const selected = ref<Record<string, unknown> | null>(null);
const loading = ref(false);
const error = ref('');
const filters = reactive({ telefone: '', tipoContratacao: '', origem: '' });

onMounted(load);

async function load() {
  loading.value = true;
  error.value = '';
  try {
    const result = await listarLeads({
      whatsApp: filters.telefone || undefined,
      tipoContratacao: filters.tipoContratacao || undefined,
      origem: filters.origem || undefined
    });
    leads.value = result.itens;
    selected.value = null;
  } catch {
    error.value = 'Nao foi possivel carregar os leads.';
  } finally {
    loading.value = false;
  }
}

async function select(id: string) {
  selected.value = await obterLead(id);
}

function dateTime(value: string) {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value));
}
</script>
