<template>
  <main class="page">
    <section class="page-header row">
      <div>
        <p class="eyebrow">Pre-publicacao</p>
        <h1>Preview Google Ads</h1>
        <p class="subtitle">Estrutura tecnica planejada, sem publicacao real.</p>
      </div>
      <div class="actions header-actions">
        <RouterLink class="button secondary" :to="`/campanhas/${campanhaId}`">Voltar</RouterLink>
        <button class="button secondary" :disabled="busy || !preview" @click="validar">Validar</button>
        <button class="button secondary" :disabled="busy || !preview" @click="sugerir">Sugerir ajustes</button>
        <button class="button secondary" :disabled="busy || !preview" @click="excluir">Excluir</button>
        <button class="button" :disabled="busy" @click="gerar">{{ preview ? 'Regenerar' : 'Gerar preview' }}</button>
      </div>
    </section>

    <p v-if="error" class="error">{{ error }}</p>
    <SkeletonBlock v-if="loading" :count="5" />

    <EmptyState v-else-if="!preview" title="Preview ainda nao gerado" message="Gere a estrutura tecnica da campanha para validar antes da publicacao futura.">
      <button class="button" :disabled="busy" @click="gerar">Gerar preview</button>
    </EmptyState>

    <template v-else>
      <section class="metrics-grid">
        <MetricCard label="Status" :value="preview.status" />
        <MetricCard label="Headlines validas" :value="preview.contadores.headlinesValidas" />
        <MetricCard label="Descriptions validas" :value="preview.contadores.descriptionsValidas" />
        <MetricCard label="Keywords" :value="preview.contadores.keywords" />
        <MetricCard label="Erros" :value="preview.contadores.erros" />
        <MetricCard label="Avisos" :value="preview.contadores.avisos" />
      </section>

      <section class="panel preview-notice">
        <span class="status" :class="statusClass(preview.status)">{{ preview.status }}</span>
        <p>As alteracoes feitas aqui afetam apenas o preview do Google Ads.</p>
      </section>

      <section class="grid-layout">
        <article class="panel">
          <header class="section-heading">
            <div>
              <h2>Campanha</h2>
              <span>{{ preview.contaNome }} / {{ preview.customerId }}</span>
            </div>
          </header>
          <label>Nome<input v-model="form.nomeCampanha" /></label>
          <label>Orcamento diario<input v-model.number="form.orcamentoDiario" type="number" min="0" step="0.01" /></label>
          <dl class="compact-list">
            <dt>Rede</dt><dd>{{ preview.tipoRede }}</dd>
            <dt>Moeda</dt><dd>{{ preview.codigoMoeda }}</dd>
            <dt>Micros</dt><dd>{{ preview.orcamentoMicros }}</dd>
            <dt>URL</dt><dd>{{ preview.urlFinal }}</dd>
            <dt>Versao</dt><dd>{{ preview.versao }}</dd>
          </dl>
        </article>

        <article class="panel">
          <header class="section-heading"><h2>Grupo de anuncios</h2></header>
          <label>Nome do grupo<input v-model="form.nomeGrupo" /></label>
          <label>CPC opcional<input v-model.number="form.cpcBid" type="number" min="0" step="0.01" /></label>
          <div class="two-cols">
            <label>Path 1<input v-model="form.path1" maxlength="15" /></label>
            <label>Path 2<input v-model="form.path2" maxlength="15" /></label>
          </div>
        </article>
      </section>

      <section class="grid-layout">
        <article class="panel">
          <header class="section-heading"><h2>Keywords</h2></header>
          <div v-for="(_, index) in form.keywords" :key="`kw-${index}`" class="inline-edit">
            <input v-model="form.keywords[index].texto" />
            <select v-model="form.keywords[index].matchType">
              <option>PHRASE</option>
              <option>EXACT</option>
              <option>BROAD</option>
            </select>
          </div>
        </article>
        <article class="panel">
          <header class="section-heading"><h2>Negativas</h2></header>
          <div v-for="(_, index) in form.negativas" :key="`neg-${index}`" class="inline-edit">
            <input v-model="form.negativas[index].texto" />
            <select v-model="form.negativas[index].matchType">
              <option>PHRASE</option>
              <option>EXACT</option>
              <option>BROAD</option>
            </select>
          </div>
        </article>
      </section>

      <section class="grid-layout">
        <article class="panel">
          <header class="section-heading"><h2>Headlines</h2></header>
          <div v-for="(_, index) in form.headlines" :key="`h-${index}`" class="inline-edit">
            <input v-model="form.headlines[index]" />
            <span class="counter">{{ form.headlines[index].length }}/30</span>
          </div>
        </article>
        <article class="panel">
          <header class="section-heading"><h2>Descriptions</h2></header>
          <div v-for="(_, index) in form.descriptions" :key="`d-${index}`" class="inline-edit">
            <textarea v-model="form.descriptions[index]" rows="2" />
            <span class="counter">{{ form.descriptions[index].length }}/90</span>
          </div>
        </article>
      </section>

      <div class="actions">
        <button class="button secondary" :disabled="busy" @click="hydrate">Cancelar</button>
        <button class="button" :disabled="busy" @click="salvar">Salvar preview</button>
      </div>

      <section class="grid-layout">
        <article class="panel">
          <h2>Erros bloqueantes</h2>
          <p v-if="preview.erros.length === 0">Nenhum erro.</p>
          <ul><li v-for="erro in preview.erros" :key="erro">{{ erro }}</li></ul>
        </article>
        <article class="panel">
          <h2>Avisos</h2>
          <p v-if="preview.avisos.length === 0">Nenhum aviso.</p>
          <ul><li v-for="aviso in preview.avisos" :key="aviso">{{ aviso }}</li></ul>
        </article>
      </section>

      <section v-if="sugestoes.length" class="panel">
        <header class="section-heading"><h2>Sugestoes IA</h2></header>
        <article v-for="item in sugestoes" :key="`${item.campo}-${item.indice}`" class="suggestion-row">
          <div>
            <strong>{{ item.campo }} #{{ item.indice + 1 }}</strong>
            <span>{{ item.original }}</span>
            <p>{{ item.sugestao }}</p>
          </div>
          <button class="button secondary" :disabled="busy" @click="aplicar(item)">Aplicar</button>
        </article>
      </section>

      <section class="panel">
        <header class="section-heading"><h2>Payload tecnico</h2></header>
        <pre class="payload-box">{{ JSON.stringify(preview.payload, null, 2) }}</pre>
      </section>
    </template>
  </main>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { useRoute } from 'vue-router';
import EmptyState from '../components/EmptyState.vue';
import MetricCard from '../components/MetricCard.vue';
import SkeletonBlock from '../components/SkeletonBlock.vue';
import { confirmAction, showToast } from '../components/uiEvents';
import {
  aplicarSugestaoGoogleAdsPreview,
  atualizarGoogleAdsPreview,
  excluirGoogleAdsPreview,
  gerarGoogleAdsPreview,
  obterGoogleAdsPreviewPorCampanha,
  sugerirAjustesGoogleAdsPreview,
  validarGoogleAdsPreview,
  type GoogleAdsPreview,
  type GoogleAdsSuggestion,
  type StatusGoogleAdsPreview
} from '../services/api';

const route = useRoute();
const campanhaId = String(route.params.id);
const preview = ref<GoogleAdsPreview | null>(null);
const sugestoes = ref<GoogleAdsSuggestion[]>([]);
const loading = ref(false);
const busy = ref(false);
const error = ref('');
const form = reactive({
  nomeCampanha: '',
  orcamentoDiario: 0,
  nomeGrupo: '',
  cpcBid: undefined as number | undefined,
  keywords: [] as Array<{ texto: string; matchType: string }>,
  negativas: [] as Array<{ texto: string; matchType: string }>,
  headlines: [] as string[],
  descriptions: [] as string[],
  path1: '',
  path2: ''
});

onMounted(load);

async function load() {
  loading.value = true;
  error.value = '';
  try {
    preview.value = await obterGoogleAdsPreviewPorCampanha(campanhaId);
    hydrate();
  } catch {
    preview.value = null;
  } finally {
    loading.value = false;
  }
}

async function gerar() {
  busy.value = true;
  error.value = '';
  try {
    preview.value = await gerarGoogleAdsPreview(campanhaId);
    sugestoes.value = [];
    hydrate();
    showToast({ type: 'success', title: 'Preview gerado' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel gerar o preview.');
    showToast({ type: 'error', title: 'Pendencias no preview', message: error.value });
  } finally {
    busy.value = false;
  }
}

function hydrate() {
  if (!preview.value) return;
  const adGroup = preview.value.payload.adGroups[0];
  form.nomeCampanha = preview.value.nomeCampanha;
  form.orcamentoDiario = preview.value.orcamentoDiario;
  form.nomeGrupo = adGroup.name;
  form.cpcBid = adGroup.cpcBid;
  form.keywords = adGroup.keywords.map((x) => ({ texto: x.text, matchType: x.matchType }));
  form.negativas = adGroup.negativeKeywords.map((x) => ({ texto: x.text, matchType: x.matchType }));
  form.headlines = [...adGroup.responsiveSearchAd.headlines];
  form.descriptions = [...adGroup.responsiveSearchAd.descriptions];
  form.path1 = adGroup.responsiveSearchAd.path1;
  form.path2 = adGroup.responsiveSearchAd.path2;
}

async function salvar() {
  if (!preview.value) return;
  busy.value = true;
  try {
    preview.value = await atualizarGoogleAdsPreview(preview.value.id, { ...form });
    hydrate();
    showToast({ type: 'success', title: 'Preview salvo' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel salvar o preview.');
  } finally {
    busy.value = false;
  }
}

async function validar() {
  if (!preview.value) return;
  busy.value = true;
  try {
    preview.value = await validarGoogleAdsPreview(preview.value.id);
    hydrate();
    showToast({ type: preview.value.erros.length ? 'error' : 'success', title: preview.value.status });
  } finally {
    busy.value = false;
  }
}

async function sugerir() {
  if (!preview.value) return;
  busy.value = true;
  try {
    const result = await sugerirAjustesGoogleAdsPreview(preview.value.id);
    sugestoes.value = result.sugestoes;
    showToast({ type: 'success', title: 'Sugestoes geradas', message: `${sugestoes.value.length} sugestoes` });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel gerar sugestoes.');
  } finally {
    busy.value = false;
  }
}

async function aplicar(item: GoogleAdsSuggestion) {
  if (!preview.value) return;
  preview.value = await aplicarSugestaoGoogleAdsPreview(preview.value.id, item);
  sugestoes.value = sugestoes.value.filter((x) => x !== item);
  hydrate();
}

async function excluir() {
  if (!preview.value) return;
  const confirmed = await confirmAction({ title: 'Excluir preview', message: 'O preview tecnico sera removido.', confirmLabel: 'Excluir' });
  if (!confirmed) return;
  await excluirGoogleAdsPreview(preview.value.id);
  preview.value = null;
  sugestoes.value = [];
}

function statusClass(status: StatusGoogleAdsPreview) {
  return `status-${status.toLowerCase()}`;
}

function message(err: unknown, fallback: string) {
  const response = err as { response?: { data?: { mensagem?: string } } };
  return response.response?.data?.mensagem || fallback;
}
</script>
