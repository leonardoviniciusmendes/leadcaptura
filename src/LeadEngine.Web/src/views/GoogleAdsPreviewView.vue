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
        <button class="button secondary" :disabled="busy || !canRemoteValidate" @click="executarDryRun">Executar dry run</button>
        <button class="button secondary" :disabled="busy || !canRemoteValidate || !dryRun?.valido" @click="validarRemoto">Validar no Google Ads</button>
        <button class="button secondary" :disabled="busy || !canPrepare" @click="prepararPublicacao">Preparar publicacao</button>
        <button class="button secondary" :disabled="busy || !preview" @click="sugerir">Sugerir ajustes</button>
        <button class="button secondary" :disabled="busy || !preview" @click="excluir">Excluir</button>
        <RouterLink v-if="publicacao" class="button secondary" :to="`/googleads/publicacoes/${publicacao.id}`">Abrir publicacao</RouterLink>
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
        <span v-if="preparacao?.teste" class="status status-gerando">Conta teste</span>
      </section>

      <section class="panel">
        <header class="section-heading"><h2>Etapas</h2></header>
        <div class="step-grid">
          <span class="status status-revisada">1. Preview local</span>
          <span class="status" :class="dryRun?.valido ? 'status-revisada' : 'status-gerada'">2. Dry run</span>
          <span class="status" :class="validacaoRemota?.valido ? 'status-revisada' : 'status-gerada'">3. Validacao no Google</span>
          <span class="status" :class="preparacao ? 'status-revisada' : 'status-gerada'">4. Preparacao</span>
          <span class="status" :class="confirmPaused ? 'status-revisada' : 'status-gerada'">5. Confirmacao</span>
          <span class="status" :class="publicacao ? 'status-revisada' : 'status-gerada'">6. Publicacao pausada</span>
          <span class="status status-gerada">7. Reconciliacao</span>
        </div>
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

      <section v-if="validacaoRemota" class="panel">
        <header class="section-heading"><h2>Validacao remota</h2></header>
        <p>RequestId: {{ validacaoRemota.requestId || '-' }}</p>
        <p>{{ validacaoRemota.valido ? 'Validacao remota aprovada.' : 'Validacao remota recusada.' }}</p>
        <ul><li v-for="erro in validacaoRemota.erros" :key="`${erro.codigo}-${erro.campo}`">{{ erro.mensagem }}</li></ul>
      </section>

      <section v-if="dryRun" class="panel">
        <header class="section-heading"><h2>Dry run</h2></header>
        <p>{{ dryRun.quantidadeOperacoes }} operacoes tipadas preparadas. Nenhuma chamada foi feita ao Google Ads.</p>
        <article v-for="op in dryRun.operacoes" :key="`${op.indice}-${op.tipo}`" class="history-item">
          <strong>#{{ op.indice }} {{ op.tipo }}</strong>
          <span>{{ op.status }} {{ op.resourceNameTemporario || '' }}</span>
        </article>
      </section>

      <section v-if="preparacao" class="panel">
        <header class="section-heading">
          <div>
            <h2>Publicacao preparada</h2>
            <span>{{ preparacao.conta }} / {{ preparacao.customerIdMascarado }}</span>
          </div>
        </header>
        <dl class="compact-list">
          <dt>Campanha</dt><dd>{{ preparacao.nome }}</dd>
          <dt>Orcamento</dt><dd>{{ money(preparacao.orcamentoDiario) }}</dd>
          <dt>Status</dt><dd>{{ preparacao.statusPlanejado }}</dd>
          <dt>Keywords</dt><dd>{{ preparacao.quantidadeKeywords }}</dd>
          <dt>Anuncios</dt><dd>{{ preparacao.quantidadeAnuncios }}</dd>
          <dt>URL</dt><dd>{{ preparacao.url }}</dd>
        </dl>
        <label class="remove-secret"><input v-model="confirmPaused" type="checkbox" /> Confirmo a criacao da campanha em estado pausado.</label>
        <p class="subtitle">Esta operacao criara recursos reais na conta Google Ads selecionada. A campanha sera criada pausada e nao comecara a gerar cobrancas ate ser ativada manualmente.</p>
        <button class="button" :disabled="busy || !confirmPaused || !preparacao.validacaoRemota" @click="publicar">Publicar como pausada</button>
      </section>

      <section v-if="publicacao" class="panel">
        <header class="section-heading"><h2>Publicacao</h2></header>
        <p>Status: <span class="status">{{ publicacao.status }}</span></p>
        <p>RequestId: {{ publicacao.requestIdPublicacao || publicacao.requestIdValidacao || '-' }}</p>
        <RouterLink class="button secondary narrow" :to="`/googleads/publicacoes/${publicacao.id}`">Ver publicacao</RouterLink>
        <article v-for="resource in publicacao.recursos" :key="resource.resourceName" class="history-item">
          <strong>{{ resource.tipoRecurso }}</strong>
          <span>{{ resource.resourceName }}</span>
        </article>
      </section>

      <section class="panel">
        <header class="section-heading"><h2>Payload tecnico</h2></header>
        <pre class="payload-box">{{ JSON.stringify(preview.payload, null, 2) }}</pre>
      </section>
    </template>

    <div v-if="diagnosticoGoogleAds" class="modal-backdrop">
      <section class="panel modal google-ads-error-modal" role="dialog" aria-modal="true">
        <header class="section-heading">
          <div>
            <p class="eyebrow">Diagnostico Google Ads</p>
            <h2>{{ diagnosticoGoogleAds.mensagem || 'Operacao recusada pelo Google Ads' }}</h2>
          </div>
          <button class="mini-button" @click="diagnosticoGoogleAds = null">Fechar</button>
        </header>
        <dl class="compact-list">
          <dt>Codigo</dt><dd>{{ diagnosticoGoogleAds.codigo || '-' }}</dd>
          <dt>RequestId</dt><dd>{{ diagnosticoGoogleAds.requestId || '-' }}</dd>
          <dt>Status RPC/HTTP</dt><dd>{{ diagnosticoGoogleAds.statusCode || '-' }}</dd>
          <dt>Detalhe</dt><dd>{{ diagnosticoGoogleAds.detail || '-' }}</dd>
        </dl>
        <section class="error-list">
          <article v-for="(erro, index) in diagnosticoGoogleAds.erros" :key="`${erro.codigo}-${index}`" class="history-item">
            <strong>{{ erro.codigo }}</strong>
            <span>{{ erro.mensagem }}</span>
            <small v-if="erro.campo">Campo: {{ erro.campo }}</small>
            <small v-if="erro.location">Location: {{ erro.location }}</small>
            <small v-if="erro.fieldPathElements?.length">Path: {{ erro.fieldPathElements.join(' > ') }}</small>
            <small v-if="erro.trigger">Trigger: {{ erro.trigger }}</small>
            <small v-if="erro.acaoSugerida">Acao sugerida: {{ erro.acaoSugerida }}</small>
          </article>
        </section>
        <pre v-if="diagnosticoGoogleAds.stackTrace" class="payload-box">{{ diagnosticoGoogleAds.stackTrace }}</pre>
      </section>
    </div>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute } from 'vue-router';
import EmptyState from '../components/EmptyState.vue';
import MetricCard from '../components/MetricCard.vue';
import SkeletonBlock from '../components/SkeletonBlock.vue';
import { confirmAction, showToast } from '../components/uiEvents';
import {
  aplicarSugestaoGoogleAdsPreview,
  atualizarGoogleAdsPreview,
  dryRunGoogleAds,
  excluirGoogleAdsPreview,
  gerarGoogleAdsPreview,
  listarPublicacoesGoogleAdsPorCampanha,
  obterGoogleAdsPreviewPorCampanha,
  prepararPublicacaoGoogleAds,
  publicarGoogleAds,
  sugerirAjustesGoogleAdsPreview,
  validarRemotamenteGoogleAds,
  validarGoogleAdsPreview,
  type GoogleAdsPreview,
  type GoogleAdsDryRun,
  type GoogleAdsPreparePublication,
  type GoogleAdsPublication,
  type GoogleAdsDiagnosticResponse,
  type GoogleAdsRemoteValidation,
  type GoogleAdsSuggestion,
  type StatusGoogleAdsPreview
} from '../services/api';

const route = useRoute();
const campanhaId = String(route.params.id);
const preview = ref<GoogleAdsPreview | null>(null);
const sugestoes = ref<GoogleAdsSuggestion[]>([]);
const validacaoRemota = ref<GoogleAdsRemoteValidation | null>(null);
const dryRun = ref<GoogleAdsDryRun | null>(null);
const preparacao = ref<GoogleAdsPreparePublication | null>(null);
const publicacao = ref<GoogleAdsPublication | null>(null);
const diagnosticoGoogleAds = ref<GoogleAdsDiagnosticResponse | null>(null);
const confirmPaused = ref(false);
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

const canRemoteValidate = computed(() => preview.value?.status === 'Valido' && !preview.value?.desatualizado);
const canPrepare = computed(() => Boolean(validacaoRemota.value?.valido && preview.value?.status === 'Valido' && !preview.value?.desatualizado));

async function load() {
  loading.value = true;
  error.value = '';
  try {
    preview.value = await obterGoogleAdsPreviewPorCampanha(campanhaId);
    await carregarPublicacaoExistente();
    hydrate();
  } catch {
    preview.value = null;
    publicacao.value = null;
  } finally {
    loading.value = false;
  }
}

async function carregarPublicacaoExistente() {
  if (!preview.value) {
    publicacao.value = null;
    return;
  }

  try {
    const publicacoes = await listarPublicacoesGoogleAdsPorCampanha(campanhaId);
    publicacao.value = publicacoes.find((item) => item.previewId === preview.value?.id) ?? null;
  } catch {
    publicacao.value = null;
  }
}

async function gerar() {
  busy.value = true;
  error.value = '';
  try {
    preview.value = await gerarGoogleAdsPreview(campanhaId);
    sugestoes.value = [];
    await carregarPublicacaoExistente();
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
    preview.value = await atualizarGoogleAdsPreview(preview.value.id, {
      nomeCampanha: form.nomeCampanha,
      orcamentoDiario: form.orcamentoDiario,
      nomeGrupo: form.nomeGrupo,
      cpcBid: form.cpcBid,
      keywords: form.keywords,
      negativas: form.negativas,
      headlines: form.headlines,
      descriptions: form.descriptions,
      path1: form.path1,
      path2: form.path2
    });
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

async function validarRemoto() {
  if (!preview.value) return;
  busy.value = true;
  try {
    validacaoRemota.value = await validarRemotamenteGoogleAds(preview.value.id);
    if (validacaoRemota.value.valido) {
      showToast({ type: 'success', title: 'Validado no Google Ads' });
    } else {
      diagnosticoGoogleAds.value = diagnosticFromRemoteValidation(validacaoRemota.value);
      showToast({ type: 'error', title: 'Validacao recusada', message: validacaoRemota.value.mensagem || 'Veja os detalhes no diagnostico.' });
    }
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel validar no Google Ads.');
    diagnosticoGoogleAds.value = diagnosticFromError(err, error.value);
  } finally {
    busy.value = false;
  }
}

async function executarDryRun() {
  if (!preview.value) return;
  busy.value = true;
  try {
    dryRun.value = await dryRunGoogleAds(preview.value.id);
    showToast({ type: dryRun.value.valido ? 'success' : 'error', title: dryRun.value.valido ? 'Dry run concluido' : 'Dry run invalido' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel executar o dry run.');
    diagnosticoGoogleAds.value = diagnosticFromError(err, error.value);
  } finally {
    busy.value = false;
  }
}

async function prepararPublicacao() {
  if (!preview.value) return;
  busy.value = true;
  try {
    preparacao.value = await prepararPublicacaoGoogleAds(preview.value.id);
    confirmPaused.value = false;
    showToast({ type: 'success', title: 'Publicacao preparada' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel preparar a publicacao.');
    diagnosticoGoogleAds.value = diagnosticFromError(err, error.value);
  } finally {
    busy.value = false;
  }
}

async function publicar() {
  if (!preview.value || !preparacao.value) return;
  busy.value = true;
  try {
    publicacao.value = await publicarGoogleAds(preview.value.id, { confirmationToken: preparacao.value.confirmationToken, confirmarCriacaoPausada: confirmPaused.value });
    showToast({ type: 'success', title: 'Campanha criada como pausada' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel publicar no Google Ads.');
    diagnosticoGoogleAds.value = diagnosticFromError(err, error.value);
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
  publicacao.value = null;
  sugestoes.value = [];
}

function statusClass(status: StatusGoogleAdsPreview) {
  return `status-${status.toLowerCase()}`;
}

function money(value: number) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
}


function message(err: unknown, fallback: string) {
  const response = err as { response?: { data?: { mensagem?: string } } };
  return response.response?.data?.mensagem || fallback;
}

function diagnosticFromRemoteValidation(result: GoogleAdsRemoteValidation): GoogleAdsDiagnosticResponse {
  return {
    sucesso: false,
    codigo: result.codigo || result.erros[0]?.codigo || 'google_ads_validation_failed',
    mensagem: result.mensagem || result.erros[0]?.mensagem || 'Validacao remota recusada pelo Google Ads.',
    requestId: result.requestId || result.erros[0]?.requestId,
    erros: result.erros,
    stackTrace: result.stackTrace
  };
}

function diagnosticFromError(err: unknown, fallback: string): GoogleAdsDiagnosticResponse {
  const response = err as { response?: { data?: Partial<GoogleAdsDiagnosticResponse> & { mensagem?: string; code?: string } } };
  const data = response.response?.data;
  return {
    sucesso: false,
    codigo: data?.codigo || data?.code || 'google_ads_error',
    mensagem: data?.mensagem || fallback,
    requestId: data?.requestId,
    erros: data?.erros?.length ? data.erros : [{
      codigo: data?.codigo || data?.code || 'google_ads_error',
      mensagem: data?.mensagem || fallback,
      recuperavel: true
    }],
    statusCode: data?.statusCode,
    detail: data?.detail,
    stackTrace: data?.stackTrace
  };
}
</script>
