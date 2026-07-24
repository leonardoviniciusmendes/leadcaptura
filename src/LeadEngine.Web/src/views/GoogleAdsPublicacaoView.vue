<template>
  <main class="page">
    <section class="page-header row">
      <div>
        <p class="eyebrow">Google Ads</p>
        <h1>Publicacao</h1>
        <p class="subtitle">Timeline e recursos criados sem expor segredos.</p>
      </div>
      <button class="button secondary" :disabled="busy || !publicacao" @click="reconciliar">Reconciliar</button>
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
          <li>Preparada</li>
          <li>Validada</li>
          <li>Publicando</li>
          <li>{{ publicacao.status }}</li>
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
import { showToast } from '../components/uiEvents';
import { obterPublicacaoGoogleAds, reconciliarPublicacaoGoogleAds, type GoogleAdsPublication } from '../services/api';

const route = useRoute();
const publicacao = ref<GoogleAdsPublication | null>(null);
const loading = ref(false);
const busy = ref(false);
const error = ref('');

onMounted(load);

async function load() {
  loading.value = true;
  try {
    publicacao.value = await obterPublicacaoGoogleAds(String(route.params.id));
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
</script>
