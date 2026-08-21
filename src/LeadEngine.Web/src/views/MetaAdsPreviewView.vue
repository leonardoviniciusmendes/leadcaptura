<template>
  <main class="page">
    <section class="page-header row">
      <div>
        <p class="eyebrow">Pre-publicacao</p>
        <h1>Preview Meta Ads</h1>
        <p class="subtitle">Campaign, Ad Set, Creative e Ad planejados para criacao inicial pausada.</p>
      </div>
      <div class="actions header-actions">
        <RouterLink class="button secondary" :to="`/campanhas/${campanhaId}`">Voltar</RouterLink>
        <button class="button secondary" :disabled="busy" @click="autorizarPublicacao">Autorizar publicacao</button>
        <button class="button" :disabled="busy || !preview?.preflight.readyToPublish || publishing" @click="publicar">
          {{ publishing ? 'Publicando...' : 'Publicar no Meta Ads' }}
        </button>
        <button class="button" :disabled="busy" @click="gerar">{{ preview ? 'Regenerar preview' : 'Gerar preview' }}</button>
      </div>
    </section>

    <p v-if="error" class="error">{{ error }}</p>
    <SkeletonBlock v-if="loading" :count="4" />

    <EmptyState v-else-if="!preview" title="Preview Meta Ads ainda nao gerado" message="Gere o preview para validar a campanha antes da futura publicacao.">
      <button class="button" :disabled="busy" @click="gerar">Gerar preview</button>
    </EmptyState>

    <template v-else>
      <section class="metrics-grid">
        <MetricCard label="Status planejado" :value="preview.campaign.status" />
        <MetricCard label="Objetivo" :value="preview.campaign.objective" />
        <MetricCard label="Otimizacao" :value="preview.adSet.optimizationGoal" />
        <MetricCard label="Preflight" :value="preview.preflight.readyToPublish ? 'Pronto' : 'Pendente'" />
      </section>

      <section class="panel preview-notice">
        <span class="status" :class="preview.preflight.readyToPublish ? 'status-revisada' : 'status-gerada'">
          {{ preview.preflight.readyToPublish ? 'ReadyToPublish' : 'Validacao pendente' }}
        </span>
        <p>O LeadEngine cria a primeira Campaign, Ad Set e Ad sempre com status PAUSED. Nenhum anuncio sera ativado automaticamente.</p>
      </section>

      <section v-if="publicacao" class="panel">
        <header class="section-heading"><h2>Publicacao Meta</h2></header>
        <dl class="compact-list">
          <dt>Status</dt><dd>{{ publicacao.status }}</dd>
          <dt>Etapa</dt><dd>{{ publicacao.ultimaEtapaConcluida }}</dd>
          <dt>Campaign ID</dt><dd>{{ publicacao.campaignExternalId || '-' }}</dd>
          <dt>Ad Set ID</dt><dd>{{ publicacao.adSetExternalId || '-' }}</dd>
          <dt>Creative ID</dt><dd>{{ publicacao.creativeExternalId || '-' }}</dd>
          <dt>Ad ID</dt><dd>{{ publicacao.adExternalId || '-' }}</dd>
          <dt>fbtrace_id</dt><dd>{{ publicacao.fbTraceId || '-' }}</dd>
        </dl>
        <p v-if="publicacao.mensagem" :class="publicacao.status === 'Concluida' ? 'success' : 'subtitle'">{{ publicacao.mensagem }}</p>
        <p v-if="publicacao.ultimoErroMensagem" class="error">{{ publicacao.ultimoErroMensagem }}</p>
        <button v-if="publicacao.podeTentarNovamente" class="button secondary" :disabled="busy || publishing" @click="retentar">Retentar publicacao</button>
      </section>

      <section class="grid-layout">
        <article class="panel">
          <header class="section-heading"><h2>Meta Ads</h2></header>
          <dl class="compact-list">
            <dt>Business</dt><dd>{{ named(preview.assets.businessNome, preview.assets.businessId) }}</dd>
            <dt>Ad Account</dt><dd>{{ named(preview.assets.adAccountNome, preview.assets.adAccountId) }}</dd>
            <dt>Facebook Page</dt><dd>{{ named(preview.assets.pageNome, preview.assets.pageId) }}</dd>
            <dt>Instagram</dt><dd>{{ named(preview.assets.instagramNome, preview.assets.instagramAccountId) }}</dd>
            <dt>Pixel/Dataset</dt><dd>{{ named(preview.assets.pixelNome, preview.assets.pixelId) }}</dd>
          </dl>
        </article>

        <article class="panel">
          <header class="section-heading"><h2>Campanha</h2></header>
          <dl class="compact-list">
            <dt>Nome</dt><dd>{{ preview.campaign.name }}</dd>
            <dt>Objetivo</dt><dd>{{ preview.campaign.objective }}</dd>
            <dt>Status</dt><dd>{{ preview.campaign.status }}</dd>
            <dt>Categoria especial</dt><dd>{{ preview.campaign.specialAdCategory }}</dd>
          </dl>
        </article>
      </section>

      <section class="grid-layout">
        <article class="panel">
          <header class="section-heading"><h2>Localizacao Meta</h2></header>
          <label>Buscar cidade ou regiao
            <input v-model="locationQuery" placeholder="Rio de Janeiro" @input="buscarLocalizacoes" />
          </label>
          <p v-if="locationMessage" class="subtitle">{{ locationMessage }}</p>
          <div v-for="item in locations" :key="item.key" class="history-item">
            <strong>{{ item.name }} <span class="status status-gerada">{{ item.type }}</span></strong>
            <span>{{ [item.region, item.countryName || item.countryCode].filter(Boolean).join(' / ') }}</span>
            <button class="mini-button" :disabled="busy" @click="selecionarLocalizacao(item)">Selecionar</button>
          </div>
        </article>

        <article class="panel">
          <header class="section-heading"><h2>Imagem Meta</h2></header>
          <input type="file" accept="image/png,image/jpeg,image/gif,image/webp" :disabled="busy" @change="uploadImagem" />
          <p v-if="uploadMessage" class="subtitle">{{ uploadMessage }}</p>
          <dl class="compact-list">
            <dt>Arquivo</dt><dd>{{ preview.creative.mediaReference || '-' }}</dd>
            <dt>Upload</dt><dd>{{ preview.creative.mediaUploaded ? 'Enviado' : 'Pendente' }}</dd>
            <dt>image_hash</dt><dd>{{ preview.creative.metaImageHash || '-' }}</dd>
          </dl>
        </article>
      </section>

      <section class="grid-layout">
        <article class="panel">
          <header class="section-heading"><h2>Orcamento e Ad Set</h2></header>
          <dl class="compact-list">
            <dt>Nome</dt><dd>{{ preview.adSet.name }}</dd>
            <dt>Valor diario</dt><dd>{{ money(preview.adSet.dailyBudget, preview.adSet.currency) }}</dd>
            <dt>Unidades Meta</dt><dd>{{ preview.adSet.dailyBudgetMinorUnits ?? '-' }}</dd>
            <dt>Billing</dt><dd>{{ preview.adSet.billingEvent }}</dd>
            <dt>Bid strategy</dt><dd>{{ preview.adSet.bidStrategy }}</dd>
          </dl>
        </article>

        <article class="panel">
          <header class="section-heading"><h2>Publico</h2></header>
          <dl class="compact-list">
            <dt>Pais</dt><dd>{{ preview.adSet.targeting.countries.join(', ') }}</dd>
            <dt>Localizacao Meta</dt><dd>{{ preview.adSet.targeting.location ? named(preview.adSet.targeting.location.name, preview.adSet.targeting.location.key) : '-' }}</dd>
            <dt>Estado/regiao original</dt><dd>{{ preview.adSet.targeting.regionText || '-' }}</dd>
            <dt>Cidade original</dt><dd>{{ preview.adSet.targeting.cityText || '-' }}</dd>
            <dt>Idade</dt><dd>{{ preview.adSet.targeting.ageMin }} - {{ preview.adSet.targeting.ageMax }}</dd>
          </dl>
        </article>
      </section>

      <section class="grid-layout">
        <article class="panel">
          <header class="section-heading"><h2>Anuncio</h2></header>
          <dl class="compact-list">
            <dt>Nome</dt><dd>{{ preview.ad.name }}</dd>
            <dt>Status</dt><dd>{{ preview.ad.status }}</dd>
            <dt>CTA</dt><dd>{{ preview.creative.callToAction }}</dd>
            <dt>Landing</dt><dd>{{ preview.creative.destinationUrl }}</dd>
          </dl>
        </article>

        <article class="panel">
          <header class="section-heading"><h2>Creative</h2></header>
          <p><strong>{{ preview.creative.headline }}</strong></p>
          <p>{{ preview.creative.primaryText }}</p>
          <p class="subtitle">{{ preview.creative.description }}</p>
          <p v-if="!preview.creative.imageUrl" class="error">Imagem/midia ainda nao configurada para Meta Ads.</p>
        </article>
      </section>

      <section class="panel">
        <header class="section-heading"><h2>Validacao</h2></header>
        <article v-for="item in preview.preflight.items" :key="item.code" class="history-item">
          <strong><span class="status" :class="preflightClass(item.status)">{{ item.status }}</span> {{ item.code }}</strong>
          <span>{{ item.message }}</span>
        </article>
      </section>

      <section class="panel">
        <header class="section-heading"><h2>Payload tecnico</h2></header>
        <pre class="payload-box">{{ JSON.stringify(preview, null, 2) }}</pre>
      </section>
    </template>
  </main>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import EmptyState from '../components/EmptyState.vue';
import MetricCard from '../components/MetricCard.vue';
import SkeletonBlock from '../components/SkeletonBlock.vue';
import { confirmAction, showToast } from '../components/uiEvents';
import {
  buscarMetaAdsLocalizacoes,
  enviarMetaAdsImagem,
  gerarMetaAdsPreview,
  obterMetaAdsAuthUrl,
  obterMetaAdsPublicacao,
  publicarMetaAds,
  retentarMetaAdsPublicacao,
  salvarMetaAdsTargeting,
  type MetaAdsLocation,
  type MetaAdsPublicacao,
  type MetaAdsPreview
} from '../services/api';

const route = useRoute();
const campanhaId = String(route.params.id);
const preview = ref<MetaAdsPreview | null>(null);
const loading = ref(false);
const busy = ref(false);
const error = ref('');
const publishing = ref(false);
const publicacao = ref<MetaAdsPublicacao | null>(null);
const locationQuery = ref('');
const locationMessage = ref('');
const locations = ref<MetaAdsLocation[]>([]);
const selectedLocationKey = ref<string | undefined>(undefined);
const uploadMessage = ref('');
let publicationPoll: number | undefined;

onMounted(async () => {
  await gerar();
  await carregarPublicacao();
});

onUnmounted(stopPublicationPoll);

async function gerar() {
  busy.value = true;
  loading.value = !preview.value;
  error.value = '';
  try {
    preview.value = await gerarMetaAdsPreview({ campanhaId, specialAdCategory: 'NONE', locationKey: selectedLocationKey.value });
    selectedLocationKey.value = preview.value.adSet.targeting.location?.key;
    showToast({ type: preview.value.preflight.readyToPublish ? 'success' : 'info', title: 'Preview Meta Ads gerado' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel gerar o preview Meta Ads.');
    showToast({ type: 'error', title: 'Preview Meta Ads', message: error.value });
  } finally {
    busy.value = false;
    loading.value = false;
  }
}

async function carregarPublicacao() {
  try {
    const result = await obterMetaAdsPublicacao(campanhaId);
    publicacao.value = result.publicacao || null;
  } catch {
    publicacao.value = null;
  }
}

async function publicar() {
  if (!preview.value?.preflight.readyToPublish || publishing.value) return;
  const confirmed = await confirmAction({
    title: 'Publicar no Meta Ads',
    message: 'Os recursos serao criados na Meta em estado PAUSADO. Nenhum anuncio sera ativado automaticamente.',
    confirmLabel: 'Criar pausado'
  });
  if (!confirmed) return;

  publishing.value = true;
  busy.value = true;
  error.value = '';
  startPublicationPoll();
  try {
    publicacao.value = await publicarMetaAds(campanhaId);
    await gerar();
    showToast({ type: publicacao.value.status === 'Concluida' ? 'success' : 'info', title: publicacao.value.mensagem });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel publicar no Meta Ads.');
    showToast({ type: 'error', title: 'Publicacao Meta Ads', message: error.value });
    await carregarPublicacao();
  } finally {
    stopPublicationPoll();
    publishing.value = false;
    busy.value = false;
  }
}

async function retentar() {
  if (!publicacao.value || publishing.value) return;
  publishing.value = true;
  busy.value = true;
  error.value = '';
  startPublicationPoll();
  try {
    publicacao.value = await retentarMetaAdsPublicacao(publicacao.value.id);
    await gerar();
    showToast({ type: publicacao.value.status === 'Concluida' ? 'success' : 'info', title: publicacao.value.mensagem });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel retentar a publicacao Meta Ads.');
  } finally {
    stopPublicationPoll();
    publishing.value = false;
    busy.value = false;
  }
}

function startPublicationPoll() {
  stopPublicationPoll();
  publicationPoll = window.setInterval(carregarPublicacao, 1500);
}

function stopPublicationPoll() {
  if (publicationPoll) {
    window.clearInterval(publicationPoll);
    publicationPoll = undefined;
  }
}

async function autorizarPublicacao() {
  busy.value = true;
  error.value = '';
  try {
    const auth = await obterMetaAdsAuthUrl(true);
    sessionStorage.setItem('metaAdsOAuthState', auth.state);
    sessionStorage.setItem('metaAdsOAuthRedirect', `/campanhas/${campanhaId}/metaads-preview`);
    window.location.href = auth.url;
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel iniciar a autorizacao Meta Ads.');
  } finally {
    busy.value = false;
  }
}

let searchTimer: number | undefined;
function buscarLocalizacoes() {
  window.clearTimeout(searchTimer);
  locationMessage.value = '';
  const query = locationQuery.value.trim();
  if (query.length < 3) {
    locations.value = [];
    return;
  }

  searchTimer = window.setTimeout(async () => {
    try {
      const result = await buscarMetaAdsLocalizacoes(query);
      locations.value = result.itens;
      locationMessage.value = result.mensagem || '';
    } catch (err: unknown) {
      locationMessage.value = message(err, 'Nao foi possivel buscar localizacoes Meta.');
    }
  }, 350);
}

async function selecionarLocalizacao(item: MetaAdsLocation) {
  busy.value = true;
  try {
    const saved = await salvarMetaAdsTargeting({ campanhaId, locationKey: item.key });
    selectedLocationKey.value = saved.key;
    locationQuery.value = saved.name;
    locations.value = [];
    await gerar();
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel salvar a localizacao Meta.');
  } finally {
    busy.value = false;
  }
}

async function uploadImagem(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;
  busy.value = true;
  uploadMessage.value = '';
  try {
    const result = await enviarMetaAdsImagem(campanhaId, file);
    uploadMessage.value = result.mensagem;
    await gerar();
  } catch (err: unknown) {
    uploadMessage.value = message(err, 'Nao foi possivel enviar a imagem para a Meta.');
  } finally {
    busy.value = false;
    input.value = '';
  }
}

function named(name?: string, id?: string) {
  if (!name && !id) return '-';
  return id ? `${name || 'Selecionado'} (${id})` : name || '-';
}

function money(value: number, currency?: string) {
  if (!currency) return `${value.toFixed(2)} - moeda nao identificada`;
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency }).format(value);
}

function preflightClass(status: string) {
  if (status === 'OK') return 'status-revisada';
  if (status === 'ERROR') return 'status-erro';
  return 'status-gerada';
}

function message(err: unknown, fallback: string) {
  const response = err as { response?: { data?: { mensagem?: string } } };
  return response.response?.data?.mensagem || fallback;
}
</script>
