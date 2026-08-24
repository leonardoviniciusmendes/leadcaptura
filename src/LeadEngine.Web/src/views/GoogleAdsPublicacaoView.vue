<template>
  <main class="page">
    <section class="page-header row">
      <div>
        <p class="eyebrow">Google Ads</p>
        <h1>Publicacao</h1>
        <p class="subtitle">Timeline e recursos criados sem expor segredos.</p>
      </div>
      <button class="button secondary" :disabled="busy || !publicacao" @click="reconciliar">Reconciliar</button>
      <button v-if="publicacao?.status === 'Reconciliada'" class="button" :disabled="busy" @click="ativar">Ativar campanha</button>
    </section>

    <p v-if="error" class="error">{{ error }}</p>
    <SkeletonBlock v-if="loading" :count="4" />
    <section v-else-if="publicacao" class="grid-layout">
      <article class="panel">
        <h2>Status</h2>
        <dl class="compact-list">
          <dt>Status</dt><dd>{{ publicacao.status }}</dd>
          <dt>Customer</dt><dd>{{ publicacao.customerIdMascarado }}</dd>
          <dt>Request validação</dt><dd>{{ publicacao.requestIdValidacao || '-' }}</dd>
          <dt>Request publicação</dt><dd>{{ publicacao.requestIdPublicacao || '-' }}</dd>
          <dt>Teste</dt><dd>{{ publicacao.teste ? 'Sim' : 'Nao' }}</dd>
        </dl>
      </article>
      <article class="panel">
        <h2>Timeline</h2>
        <ol class="timeline">
          <li v-for="item in historico" :key="item.id">
            {{ item.statusNovo }} <small>{{ item.operacao }} - {{ formatDate(item.data) }}</small>
          </li>
        </ol>
      </article>
      <article class="panel">
        <h2>Recursos</h2>
        <article v-for="item in publicacao.recursos" :key="item.resourceName" class="history-item">
          <strong>{{ item.tipoRecurso }}</strong>
          <span>{{ item.resourceName }}</span>
        </article>
      </article>
      <article class="panel">
        <h2>Erros</h2>
        <p v-if="publicacao.erros.length === 0">Nenhum erro registrado.</p>
        <ul><li v-for="erro in publicacao.erros" :key="`${erro.codigo}-${erro.operacao}`">{{ erro.mensagem }}</li></ul>
      </article>
    </section>
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import SkeletonBlock from '../components/SkeletonBlock.vue';
import { confirmAction, showToast } from '../components/uiEvents';
import { ativarPublicacaoGoogleAds, historicoPublicacaoGoogleAds, obterPublicacaoGoogleAds, reconciliarPublicacaoGoogleAds, type GoogleAdsPublication, type GoogleAdsPublicationHistory } from '../services/api';

const route = useRoute();
const publicacao = ref<GoogleAdsPublication | null>(null);
const historico = ref<GoogleAdsPublicationHistory[]>([]);
const loading = ref(false);
const busy = ref(false);
const error = ref('');

onMounted(load);

async function load() {
  loading.value = true;
  try {
    publicacao.value = await obterPublicacaoGoogleAds(String(route.params.id));
    historico.value = await historicoPublicacaoGoogleAds(String(route.params.id));
  } catch {
    error.value = 'Nao foi possivel carregar a publicacao.';
  } finally {
    loading.value = false;
  }
}

async function reconciliar() {
  if (!publicacao.value) return;
  busy.value = true;
  try {
    await reconciliarPublicacaoGoogleAds(publicacao.value.id);
    await load();
    showToast({ type: 'success', title: 'Reconciliacao concluida' });
  } finally {
    busy.value = false;
  }
}

async function ativar() {
  if (!publicacao.value || publicacao.value.status !== 'Reconciliada') return;
  const confirmed = await confirmAction({
    title: 'Ativar campanha Google Ads',
    message: 'Confirme a ativacao dos recursos desta publicacao na conta de teste configurada. A campanha podera comecar a veicular.',
    confirmLabel: 'Ativar'
  });
  if (!confirmed) return;
  busy.value = true;
  error.value = '';
  try {
    publicacao.value = await ativarPublicacaoGoogleAds(publicacao.value.id);
    historico.value = await historicoPublicacaoGoogleAds(publicacao.value.id);
    showToast({ type: 'success', title: 'Ativacao enviada', message: 'Recursos desta publicacao foram ativados.' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel ativar a campanha.');
    showToast({ type: 'error', title: 'Ativacao bloqueada', message: error.value });
  } finally {
    busy.value = false;
  }
}

function message(err: unknown, fallback: string) {
  const response = err as { response?: { data?: { mensagem?: string; message?: string; detail?: string } } };
  return response.response?.data?.mensagem || response.response?.data?.message || response.response?.data?.detail || fallback;
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value));
}
</script>
