<template>
  <main class="page">
    <section class="page-header row">
      <div>
        <p class="eyebrow">Google Ads</p>
        <h1>Visao geral</h1>
        <p class="subtitle">Metricas sincronizadas, leads atribuidos e analise assistida.</p>
      </div>
      <div class="actions header-actions">
        <select v-model="periodo" @change="applyPeriod">
          <option value="7">7 dias</option>
          <option value="30">30 dias</option>
          <option value="custom">Personalizado</option>
        </select>
        <input v-if="periodo === 'custom'" v-model="dataInicial" type="date" />
        <input v-if="periodo === 'custom'" v-model="dataFinal" type="date" />
        <button class="button secondary" :disabled="loading" @click="load">Atualizar</button>
        <button class="button" :disabled="loading" @click="syncNow">Sincronizar agora</button>
      </div>
    </section>

    <p v-if="error" class="error">{{ error }}</p>
    <SkeletonBlock v-if="loading" :count="5" />

    <template v-else-if="resumo">
      <section class="metrics-grid">
        <MetricCard label="Impressoes" :value="resumo.impressoes" />
        <MetricCard label="Cliques" :value="resumo.cliques" />
        <MetricCard label="CTR" :value="`${resumo.ctr}%`" />
        <MetricCard label="Custo" :value="money(resumo.custo)" />
        <MetricCard label="CPC medio" :value="money(resumo.cpcMedio)" />
        <MetricCard label="Conversoes" :value="resumo.conversoes" />
        <MetricCard label="Leads" :value="resumo.leads" />
        <MetricCard label="CPL" :value="money(resumo.custoPorLead)" />
      </section>

      <section class="grid-layout">
        <article class="panel">
          <header class="section-heading"><h2>Evolucao</h2></header>
          <div class="chart-bars">
            <div v-for="row in evolucao" :key="row.data" class="chart-day">
              <span :style="{ height: bar(row.cliques) }" />
              <small>{{ shortDate(row.data) }}</small>
            </div>
          </div>
        </article>
        <article class="panel">
          <header class="section-heading"><h2>Resumo comercial</h2></header>
          <dl class="compact-list">
            <dt>Taxa conversao</dt><dd>{{ resumo.taxaConversao }}%</dd>
            <dt>ROAS</dt><dd>{{ resumo.roas }}</dd>
            <dt>Qualidade atribuicao</dt><dd>{{ resumo.qualidadeAtribuicao }}</dd>
            <dt>Ultima sincronizacao</dt><dd>{{ resumo.ultimaSincronizacao ? formatDate(resumo.ultimaSincronizacao) : '-' }}</dd>
          </dl>
        </article>
      </section>

      <section class="panel">
        <header class="section-heading"><h2>Campanhas</h2></header>
        <div class="table-wrap">
          <table>
            <thead><tr><th>Campanha</th><th>Status</th><th>Impressoes</th><th>Cliques</th><th>CTR</th><th>Custo</th><th>Conversoes</th><th>Leads</th><th>CPL</th><th></th></tr></thead>
            <tbody>
              <tr v-for="item in campanhas" :key="item.publicacaoId">
                <td>{{ item.campanha }}</td>
                <td>{{ item.status }}</td>
                <td>{{ item.impressoes }}</td>
                <td>{{ item.cliques }}</td>
                <td>{{ item.ctr }}%</td>
                <td>{{ money(item.custo) }}</td>
                <td>{{ item.conversoes }}</td>
                <td>{{ item.leads }}</td>
                <td>{{ money(item.custoPorLead) }}</td>
                <td><button class="button secondary narrow" @click="analisar(item.publicacaoId)">Analisar</button></td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <section v-if="analise" class="panel">
        <header class="section-heading"><h2>Analise IA</h2></header>
        <p>{{ analise.resumo }}</p>
        <ul><li v-for="acao in analise.resultado.acoesPrioritarias" :key="acao">{{ acao }}</li></ul>
        <p class="subtitle">Sugestoes nao sao aplicadas automaticamente. Gere um novo preview pela pagina da publicacao quando aprovar manualmente.</p>
      </section>
    </template>
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import MetricCard from '../components/MetricCard.vue';
import SkeletonBlock from '../components/SkeletonBlock.vue';
import { showToast } from '../components/uiEvents';
import { analisarGoogleAdsPublicacao, obterGoogleAdsDashboard, obterGoogleAdsDashboardCampanhas, obterGoogleAdsDashboardEvolucao, sincronizarGoogleAdsMetricas, type GoogleAdsAnalise, type GoogleAdsDashboardCampanha, type GoogleAdsDashboardResumo, type GoogleAdsEvolucao } from '../services/api';

const resumo = ref<GoogleAdsDashboardResumo | null>(null);
const campanhas = ref<GoogleAdsDashboardCampanha[]>([]);
const evolucao = ref<GoogleAdsEvolucao[]>([]);
const analise = ref<GoogleAdsAnalise | null>(null);
const loading = ref(false);
const error = ref('');
const periodo = ref('30');
const dataInicial = ref('');
const dataFinal = ref('');

onMounted(() => { applyPeriod(); void load(); });

function params() { return { dataInicial: dataInicial.value || undefined, dataFinal: dataFinal.value || undefined }; }
function applyPeriod() {
  if (periodo.value === 'custom') return;
  const end = new Date();
  const start = new Date();
  start.setDate(end.getDate() - Number(periodo.value) + 1);
  dataInicial.value = start.toISOString().slice(0, 10);
  dataFinal.value = end.toISOString().slice(0, 10);
}
async function load() {
  loading.value = true; error.value = '';
  try {
    const [r, c, e] = await Promise.all([obterGoogleAdsDashboard(params()), obterGoogleAdsDashboardCampanhas(params()), obterGoogleAdsDashboardEvolucao(params())]);
    resumo.value = r; campanhas.value = c; evolucao.value = e;
  } catch { error.value = 'Nao foi possivel carregar o dashboard Google Ads.'; }
  finally { loading.value = false; }
}
async function syncNow() {
  loading.value = true;
  try { await sincronizarGoogleAdsMetricas(dataInicial.value, dataFinal.value); await load(); showToast({ type: 'success', title: 'Sincronizacao concluida' }); }
  catch { error.value = 'Nao foi possivel sincronizar metricas.'; }
  finally { loading.value = false; }
}
async function analisar(id: string) {
  analise.value = await analisarGoogleAdsPublicacao(id, dataInicial.value, dataFinal.value);
}
function money(value: number) { return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value || 0); }
function shortDate(value: string) { return new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit' }).format(new Date(value)); }
function formatDate(value: string) { return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value)); }
function bar(value: number) { const max = Math.max(...evolucao.value.map((x) => x.cliques), 1); return `${Math.max(8, (value / max) * 120)}px`; }
</script>
