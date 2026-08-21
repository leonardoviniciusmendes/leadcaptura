<template>
  <main class="page">
    <section class="page-header">
      <p class="eyebrow">Integracoes</p>
      <h1>Configuracoes</h1>
      <p class="subtitle">Administre parametros operacionais sem expor chaves sensiveis.</p>
    </section>

    <section v-if="status" class="metrics-grid">
      <MetricCard label="OpenRouter" :value="status.openRouter.status" />
      <MetricCard label="WhatsApp" :value="status.whatsApp.status" />
      <MetricCard label="API externa" :value="status.externalLeadApi.status" />
      <MetricCard label="URL publica" :value="status.urlPublica.status" />
      <MetricCard label="Google Ads" :value="status.googleAds.status" />
      <MetricCard label="Meta Ads" :value="status.metaAds.status" />
    </section>

    <p v-if="oauthLoading" class="status-line">Concluindo conexao OAuth...</p>
    <p v-if="error" class="error">{{ error }}</p>

    <section class="settings-layout">
      <aside class="panel settings-nav">
        <button
          v-for="categoria in categorias"
          :key="categoria"
          :class="{ active: selected === categoria }"
          @click="selected = categoria"
        >
          {{ labelCategoria(categoria) }}
        </button>
      </aside>

      <section class="panel settings-panel">
        <SkeletonBlock v-if="loading" :count="6" />
        <template v-else-if="current">
          <header class="section-heading">
            <div>
              <h2>{{ labelCategoria(current.categoria) }}</h2>
              <span>{{ description(current.categoria) }}</span>
            </div>
            <button class="button secondary" :disabled="saving || testing" @click="testar">{{ testing ? 'Testando...' : 'Testar' }}</button>
          </header>

          <form class="settings-form" @submit.prevent="salvar">
            <div v-for="item in current.configuracoes" :key="item.chave" class="setting-row">
              <div>
                <label :for="item.chave">{{ item.chave }}</label>
                <small>{{ item.descricao }} · origem: {{ item.origem }}</small>
              </div>
              <div v-if="item.sensivel" class="secret-control">
                <span class="status" :class="item.configurado ? 'status-revisada' : 'status-erro'">
                  {{ item.configurado ? 'Configurado' : 'Nao configurado' }}
                </span>
                <input :id="item.chave" v-model="form[item.chave]" type="password" placeholder="Substituir segredo" autocomplete="new-password" />
                <label class="remove-secret"><input v-model="removeFlags[item.chave]" type="checkbox" /> Remover</label>
              </div>
              <input v-else :id="item.chave" v-model="form[item.chave]" />
            </div>

            <div class="actions">
              <button class="button secondary" type="button" :disabled="saving" @click="load">Cancelar</button>
              <button class="button" :disabled="saving">{{ saving ? 'Salvando...' : 'Salvar' }}</button>
            </div>
          </form>

          <section v-if="selected === 'GoogleAds'" class="google-ads-panel">
            <article v-if="googleAmbiente" class="integration-banner">
              <div>
                <strong>Ambiente Google Ads: {{ googleAmbiente.modo }}</strong>
                <span>CustomerId teste: {{ googleAmbiente.customerIdMascarado || '-' }}</span>
              </div>
              <span class="status" :class="googleAmbiente.publicacaoPermitida ? 'status-revisada' : 'status-erro'">
                {{ googleAmbiente.publicacaoPermitida ? 'Publicacao real habilitada' : 'Publicacao real bloqueada' }}
              </span>
            </article>
            <ul v-if="googleAmbiente?.pendencias.length" class="compact-errors">
              <li v-for="pendencia in googleAmbiente.pendencias" :key="pendencia">{{ pendencia }}</li>
            </ul>
            <header class="section-heading compact">
              <div>
                <h3>Conexao Google Ads</h3>
                <span>{{ googleStatus?.status || 'Nao conectado' }}</span>
              </div>
              <div class="actions inline">
                <button class="button secondary" :disabled="googleBusy || saving || testing" @click="conectarGoogle">
                  {{ googleBusy ? 'Conectando...' : 'Conectar conta Google' }}
                </button>
                <button class="button secondary" :disabled="googleBusy || !googleContas.length" @click="testarConexaoGoogle">
                  Testar conexao
                </button>
              </div>
            </header>

            <div v-if="googleContas.length" class="accounts-list">
              <article v-for="conta in googleContas" :key="conta.id" class="account-card">
                <div>
                  <strong>{{ conta.nome }}</strong>
                  <span>{{ conta.customerIdMascarado || conta.customerId }} · {{ conta.tipoConta }} <template v-if="conta.gerente">· gerente</template><template v-if="conta.email"> · {{ conta.email }}</template></span>
                </div>
                <button class="button secondary" :disabled="googleBusy || conta.padrao" @click="selecionarConta(conta.id)">
                  {{ conta.padrao ? 'Conta padrao' : 'Selecionar' }}
                </button>
              </article>
            </div>
            <EmptyState v-else title="Nenhuma conta conectada" message="Conecte uma conta Google para preparar a publicacao futura no Google Ads." />
          </section>

          <section v-if="selected === 'MetaAds'" class="google-ads-panel">
            <article class="integration-banner">
              <div>
                <strong>Meta Ads: {{ metaStatus?.status || status?.metaAds.status || 'Configuracao inicial pendente' }}</strong>
                <span v-if="metaStatus?.conectado">{{ metaStatus.nome || metaStatus.metaUserId }}<template v-if="metaStatus.dataConexao"> · conectado em {{ formatDate(metaStatus.dataConexao) }}</template></span>
                <span v-else>Configure AppId, AppSecret e RedirectUri para conectar a conta Meta.</span>
              </div>
              <span class="status" :class="metaStatus?.conectado ? 'status-revisada' : (metaStatus?.configurado || status?.metaAds.configurado ? 'status-gerada' : 'status-erro')">
                {{ metaStatus?.conectado ? 'Conectado' : (metaStatus?.configurado || status?.metaAds.configurado ? 'Configurado' : 'Pendente') }}
              </span>
            </article>
            <header class="section-heading compact">
              <div>
                <h3>Conexao Meta Ads</h3>
                <span>{{ metaStatus?.conectado ? 'OAuth conectado' : 'Sem conta Meta conectada' }}</span>
              </div>
              <div class="actions inline">
                <button v-if="!metaStatus?.conectado" class="button secondary" :disabled="metaBusy || saving || testing || !(metaStatus?.configurado || status?.metaAds.configurado)" @click="conectarMeta">
                  {{ metaBusy ? 'Conectando...' : 'Conectar Meta Ads' }}
                </button>
                <button v-else class="button secondary" :disabled="metaBusy" @click="desconectarMeta">
                  {{ metaBusy ? 'Desconectando...' : 'Desconectar' }}
                </button>
                <button class="button secondary" disabled>Testar conexao</button>
              </div>
            </header>
            <EmptyState v-if="!metaStatus?.conectado" title="Nenhuma conta Meta conectada" message="A selecao de Business, conta de anuncios, pagina, Instagram e pixel sera adicionada em etapa futura." />
            <section v-else class="settings-form">
              <p v-if="metaPermissionNeeded" class="error">Permissoes Meta insuficientes. Reconecte Meta Ads para autorizar os novos acessos de leitura.</p>
              <p v-else-if="metaAssetsMessage" class="subtitle">{{ metaAssetsMessage }}</p>

              <div class="setting-row">
                <div>
                  <label for="metaBusiness">Business</label>
                  <small>Business Portfolio acessivel pelo usuario conectado.</small>
                </div>
                <select id="metaBusiness" v-model="metaSelectionForm.businessId" :disabled="metaAssetsLoading" @change="onMetaBusinessChange">
                  <option value="">Selecionar Business</option>
                  <option v-for="item in metaBusinesses" :key="item.id" :value="item.id">{{ item.nome }}</option>
                </select>
              </div>

              <div class="setting-row">
                <div>
                  <label for="metaAdAccount">Ad Account</label>
                  <small>Conta de anuncios vinculada ao Business selecionado.</small>
                </div>
                <select id="metaAdAccount" v-model="metaSelectionForm.adAccountId" :disabled="metaAssetsLoading || !metaSelectionForm.businessId" @change="onMetaAdAccountChange">
                  <option value="">Selecionar Ad Account</option>
                  <option v-for="item in metaAdAccounts" :key="item.id" :value="item.id">{{ item.nome }}{{ item.moeda ? ` · ${item.moeda}` : '' }}</option>
                </select>
              </div>

              <div class="setting-row">
                <div>
                  <label for="metaPage">Facebook Page</label>
                  <small>Page acessivel pelo usuario conectado.</small>
                </div>
                <select id="metaPage" v-model="metaSelectionForm.pageId" :disabled="metaAssetsLoading" @change="onMetaPageChange">
                  <option value="">Selecionar Page</option>
                  <option v-for="item in metaPages" :key="item.id" :value="item.id">{{ item.nome }}</option>
                </select>
              </div>

              <div class="setting-row">
                <div>
                  <label>Instagram</label>
                  <small>Instagram Professional vinculado a Page selecionada.</small>
                </div>
                <span class="status" :class="selectedInstagram ? 'status-revisada' : 'status-gerada'">
                  {{ selectedInstagram ? (selectedInstagram.username || selectedInstagram.nome || selectedInstagram.id) : 'Nao vinculado' }}
                </span>
              </div>

              <div class="setting-row">
                <div>
                  <label for="metaPixel">Pixel/Dataset</label>
                  <small>Obtido pela Ad Account quando disponivel para leitura.</small>
                </div>
                <select id="metaPixel" v-model="metaSelectionForm.pixelId" :disabled="metaAssetsLoading || !metaSelectionForm.adAccountId">
                  <option value="">Sem Pixel/Dataset selecionado</option>
                  <option v-for="item in metaPixels" :key="item.id" :value="item.id">{{ item.nome }}</option>
                </select>
              </div>

              <div class="actions">
                <button class="button secondary" :disabled="metaAssetsLoading" @click="loadMetaAssets">Atualizar ativos</button>
                <button class="button" :disabled="metaAssetsLoading || metaPermissionNeeded" @click="salvarMetaSelection">{{ metaAssetsLoading ? 'Salvando...' : 'Salvar ativos Meta' }}</button>
              </div>
            </section>
          </section>
        </template>
      </section>
    </section>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import MetricCard from '../components/MetricCard.vue';
import SkeletonBlock from '../components/SkeletonBlock.vue';
import EmptyState from '../components/EmptyState.vue';
import { confirmAction, showToast } from '../components/uiEvents';
import {
  concluirGoogleAdsOAuth,
  concluirMetaAdsOAuth,
  desconectarMetaAds,
  listarMetaAdsAdAccounts,
  listarMetaAdsBusinesses,
  listarMetaAdsPages,
  listarMetaAdsPixels,
  listarGoogleAdsContas,
  obterGoogleAdsAuthUrl,
  obterGoogleAdsAmbiente,
  obterGoogleAdsStatus,
  obterConfiguracaoCategoria,
  obterMetaAdsAuthUrl,
  obterMetaAdsAssetSelection,
  obterMetaAdsStatus,
  obterStatusConfiguracoes,
  selecionarGoogleAdsConta,
  salvarMetaAdsAssetSelection,
  salvarConfiguracaoCategoria,
  testarGoogleAds,
  testarConfiguracao,
  type CategoriaConfiguracao,
  type ConfiguracaoCategoria,
  type ConfiguracoesStatus,
  type GoogleAdsAmbiente,
  type GoogleAdsConta,
  type GoogleAdsStatus,
  type MetaAdsAdAccount,
  type MetaAdsAssetListResponse,
  type MetaAdsAssetSelection,
  type MetaAdsBusiness,
  type MetaAdsPage,
  type MetaAdsPixel,
  type MetaAdsStatus
} from '../services/api';

const categorias: CategoriaConfiguracao[] = ['OpenRouter', 'CampaignGeneration', 'WhatsApp', 'LeadCapture', 'ExternalLeadApi', 'Application', 'Landing', 'GoogleAds', 'MetaAds'];
const route = useRoute();
const router = useRouter();
const selected = ref<CategoriaConfiguracao>('OpenRouter');
const configs = reactive<Partial<Record<CategoriaConfiguracao, ConfiguracaoCategoria>>>({});
const status = ref<ConfiguracoesStatus | null>(null);
const googleStatus = ref<GoogleAdsStatus | null>(null);
const googleAmbiente = ref<GoogleAdsAmbiente | null>(null);
const googleContas = ref<GoogleAdsConta[]>([]);
const metaStatus = ref<MetaAdsStatus | null>(null);
const metaSelection = ref<MetaAdsAssetSelection | null>(null);
const metaBusinesses = ref<MetaAdsBusiness[]>([]);
const metaAdAccounts = ref<MetaAdsAdAccount[]>([]);
const metaPages = ref<MetaAdsPage[]>([]);
const metaPixels = ref<MetaAdsPixel[]>([]);
const loading = ref(false);
const saving = ref(false);
const testing = ref(false);
const googleBusy = ref(false);
const metaBusy = ref(false);
const oauthLoading = ref(false);
const metaAssetsLoading = ref(false);
const metaAssetsMessage = ref('');
const metaPermissionNeeded = ref(false);
const error = ref('');
const form = reactive<Record<string, string>>({});
const removeFlags = reactive<Record<string, boolean>>({});
const metaSelectionForm = reactive({ businessId: '', adAccountId: '', pageId: '', pixelId: '' });

const current = computed(() => configs[selected.value]);
const selectedInstagram = computed(() => metaPages.value.find((x) => x.id === metaSelectionForm.pageId)?.instagram ?? null);

onMounted(async () => {
  await handleGoogleCallback();
  await handleMetaCallback();
  await load();
});
watch(selected, async () => {
  if (!configs[selected.value]) {
    await load();
    return;
  }
  hydrate();
});
watch(selected, async (value) => {
  if (value === 'GoogleAds') {
    await loadGoogleAds();
  }
  if (value === 'MetaAds') {
    await loadMetaAds();
  }
});

async function load() {
  loading.value = true;
  error.value = '';
  try {
    const [categoria, statusResult] = await Promise.all([
      obterConfiguracaoCategoria(selected.value),
      obterStatusConfiguracoes()
    ]);
    configs[selected.value] = categoria;
    status.value = statusResult;
    hydrate();
    if (selected.value === 'GoogleAds') {
      await loadGoogleAds();
    }
    if (selected.value === 'MetaAds') {
      await loadMetaAds();
    }
  } catch {
    error.value = `Nao foi possivel carregar as configuracoes de ${labelCategoria(selected.value)}.`;
  } finally {
    loading.value = false;
  }
}

async function loadMetaAds() {
  try {
    metaStatus.value = await obterMetaAdsStatus();
    if (metaStatus.value.conectado) {
      await loadMetaAssets();
    }
  } catch {
    metaStatus.value = { configurado: false, conectado: false, contaSelecionada: false, status: 'Erro' };
  }
}

async function loadMetaAssets() {
  metaAssetsLoading.value = true;
  metaAssetsMessage.value = '';
  metaPermissionNeeded.value = false;
  try {
    metaSelection.value = await obterMetaAdsAssetSelection();
    metaSelectionForm.businessId = metaSelection.value.businessId || '';
    metaSelectionForm.adAccountId = metaSelection.value.adAccountId || '';
    metaSelectionForm.pageId = metaSelection.value.pageId || '';
    metaSelectionForm.pixelId = metaSelection.value.pixelId || '';

    const [businesses, pages] = await Promise.all([listarMetaAdsBusinesses(), listarMetaAdsPages()]);
    applyMetaList(businesses, metaBusinesses);
    applyMetaList(pages, metaPages);

    if (metaSelectionForm.businessId) {
      applyMetaList(await listarMetaAdsAdAccounts(metaSelectionForm.businessId), metaAdAccounts);
    } else {
      metaAdAccounts.value = [];
    }

    if (metaSelectionForm.adAccountId) {
      applyMetaList(await listarMetaAdsPixels(metaSelectionForm.adAccountId), metaPixels);
    } else {
      metaPixels.value = [];
    }
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel carregar ativos Meta.');
  } finally {
    metaAssetsLoading.value = false;
  }
}

async function onMetaBusinessChange() {
  metaSelectionForm.adAccountId = '';
  metaSelectionForm.pixelId = '';
  metaAdAccounts.value = [];
  metaPixels.value = [];
  if (!metaSelectionForm.businessId) return;
  metaAssetsLoading.value = true;
  try {
    applyMetaList(await listarMetaAdsAdAccounts(metaSelectionForm.businessId), metaAdAccounts);
  } finally {
    metaAssetsLoading.value = false;
  }
}

async function onMetaAdAccountChange() {
  metaSelectionForm.pixelId = '';
  metaPixels.value = [];
  if (!metaSelectionForm.adAccountId) return;
  metaAssetsLoading.value = true;
  try {
    applyMetaList(await listarMetaAdsPixels(metaSelectionForm.adAccountId), metaPixels);
  } finally {
    metaAssetsLoading.value = false;
  }
}

function onMetaPageChange() {
  // Instagram e derivado da Page retornada pela API Meta.
}

async function salvarMetaSelection() {
  metaAssetsLoading.value = true;
  error.value = '';
  try {
    metaSelection.value = await salvarMetaAdsAssetSelection({
      businessId: metaSelectionForm.businessId || undefined,
      adAccountId: metaSelectionForm.adAccountId || undefined,
      pageId: metaSelectionForm.pageId || undefined,
      pixelId: metaSelectionForm.pixelId || undefined
    });
    metaStatus.value = await obterMetaAdsStatus();
    status.value = await obterStatusConfiguracoes();
    showToast({ type: 'success', title: 'Ativos Meta salvos' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel salvar os ativos Meta.');
    showToast({ type: 'error', title: 'Erro ao salvar', message: error.value });
  } finally {
    metaAssetsLoading.value = false;
  }
}

async function loadGoogleAds() {
  try {
    const [statusResult, contasResult, ambienteResult] = await Promise.all([obterGoogleAdsStatus(), listarGoogleAdsContas(), obterGoogleAdsAmbiente()]);
    googleStatus.value = statusResult;
    googleContas.value = contasResult;
    googleAmbiente.value = ambienteResult;
  } catch {
    googleStatus.value = { conectado: false, status: 'Erro' };
    googleAmbiente.value = { modo: 'Indisponivel', contaCompativel: false, publicacaoPermitida: false, pendencias: ['Nao foi possivel carregar o ambiente Google Ads.'] };
    googleContas.value = [];
  }
}

function hydrate() {
  Object.keys(form).forEach((key) => delete form[key]);
  Object.keys(removeFlags).forEach((key) => delete removeFlags[key]);
  current.value?.configuracoes.forEach((item) => {
    form[item.chave] = item.sensivel ? '' : String(item.valor ?? '');
    removeFlags[item.chave] = false;
  });
}

async function conectarGoogle() {
  googleBusy.value = true;
  error.value = '';
  try {
    const result = await obterGoogleAdsAuthUrl();
    window.sessionStorage.setItem('googleAdsOAuthState', result.state);
    window.location.href = result.url;
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel iniciar o OAuth Google Ads.');
    showToast({ type: 'error', title: 'Erro ao conectar', message: error.value });
  } finally {
    googleBusy.value = false;
  }
}

async function conectarMeta() {
  metaBusy.value = true;
  error.value = '';
  try {
    const result = await obterMetaAdsAuthUrl();
    window.sessionStorage.setItem('metaAdsOAuthState', result.state);
    window.location.href = result.url;
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel iniciar o OAuth Meta Ads.');
    showToast({ type: 'error', title: 'Erro ao conectar', message: error.value });
  } finally {
    metaBusy.value = false;
  }
}

async function handleGoogleCallback() {
  const googleCallback = route.query.googleAdsCallback;
  const code = typeof route.query.code === 'string' ? route.query.code : '';
  const state = typeof route.query.state === 'string' ? route.query.state : '';
  if (googleCallback !== '1') return;
  selected.value = 'GoogleAds';
  if (!code || !state) {
    error.value = 'Callback OAuth incompleto. Tente conectar novamente.';
    await limparOAuthUrl();
    return;
  }

  const expectedState = window.sessionStorage.getItem('googleAdsOAuthState');
  if (expectedState && expectedState !== state) {
    error.value = 'State OAuth invalido. Tente conectar novamente.';
    showToast({ type: 'error', title: 'OAuth invalido', message: 'State recebido nao confere.' });
    await limparOAuthUrl();
    return;
  }

  googleBusy.value = true;
  oauthLoading.value = true;
  try {
    const result = await concluirGoogleAdsOAuth({ code, state });
    googleContas.value = result.contas;
    await Promise.all([loadGoogleAds(), load()]);
    showToast({ type: 'success', title: result.mensagem || 'Google Ads conectado com sucesso' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel concluir a conexao Google Ads.');
    showToast({ type: 'error', title: 'Erro no OAuth', message: error.value });
  } finally {
    googleBusy.value = false;
    oauthLoading.value = false;
    window.sessionStorage.removeItem('googleAdsOAuthState');
    await limparOAuthUrl();
  }
}

async function handleMetaCallback() {
  const metaCallback = route.query.metaAdsCallback;
  const code = typeof route.query.code === 'string' ? route.query.code : '';
  const state = typeof route.query.state === 'string' ? route.query.state : '';
  if (metaCallback !== '1') return;
  selected.value = 'MetaAds';
  if (!code || !state) {
    error.value = 'Callback OAuth Meta incompleto. Tente conectar novamente.';
    await limparOAuthUrl();
    return;
  }

  const expectedState = window.sessionStorage.getItem('metaAdsOAuthState');
  if (expectedState && expectedState !== state) {
    error.value = 'State OAuth Meta invalido. Tente conectar novamente.';
    showToast({ type: 'error', title: 'OAuth invalido', message: 'State recebido nao confere.' });
    await limparOAuthUrl();
    return;
  }

  metaBusy.value = true;
  oauthLoading.value = true;
  try {
    const result = await concluirMetaAdsOAuth({ code, state });
    metaStatus.value = result.status;
    await Promise.all([loadMetaAds(), load()]);
    showToast({ type: 'success', title: result.mensagem || 'Meta Ads conectado com sucesso' });
    const redirect = window.sessionStorage.getItem('metaAdsOAuthRedirect');
    if (redirect) {
      window.sessionStorage.removeItem('metaAdsOAuthRedirect');
      await router.replace(redirect);
      return;
    }
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel concluir a conexao Meta Ads.');
    showToast({ type: 'error', title: 'Erro no OAuth', message: error.value });
  } finally {
    metaBusy.value = false;
    oauthLoading.value = false;
    window.sessionStorage.removeItem('metaAdsOAuthState');
    await limparOAuthUrl();
  }
}

async function limparOAuthUrl() {
  await router.replace({ path: '/configuracoes', query: {} });
}

async function selecionarConta(id: string) {
  googleBusy.value = true;
  try {
    await selecionarGoogleAdsConta(id);
    await loadGoogleAds();
    showToast({ type: 'success', title: 'Conta padrao atualizada' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel selecionar a conta.');
    showToast({ type: 'error', title: 'Erro ao selecionar', message: error.value });
  } finally {
    googleBusy.value = false;
  }
}

async function testarConexaoGoogle() {
  googleBusy.value = true;
  try {
    const result = await testarGoogleAds(googleStatus.value?.contaPadraoId);
    showToast({ type: result.sucesso ? 'success' : 'error', title: result.status, message: result.customerId });
    await loadGoogleAds();
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel testar a conexao Google Ads.');
    showToast({ type: 'error', title: 'Erro no teste', message: error.value });
  } finally {
    googleBusy.value = false;
  }
}

async function desconectarMeta() {
  const confirmed = await confirmAction({
    title: 'Desconectar Meta Ads',
    message: 'A conexao local sera removida. O acesso remoto no Meta Developers nao sera revogado nesta etapa.',
    confirmLabel: 'Desconectar'
  });
  if (!confirmed) return;

  metaBusy.value = true;
  try {
    metaStatus.value = await desconectarMetaAds();
    metaSelection.value = null;
    metaBusinesses.value = [];
    metaAdAccounts.value = [];
    metaPages.value = [];
    metaPixels.value = [];
    status.value = await obterStatusConfiguracoes();
    showToast({ type: 'info', title: 'Meta Ads desconectado' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel desconectar Meta Ads.');
    showToast({ type: 'error', title: 'Erro ao desconectar', message: error.value });
  } finally {
    metaBusy.value = false;
  }
}

async function salvar() {
  const payload: Record<string, unknown> = {};
  current.value?.configuracoes.forEach((item) => {
    if (item.sensivel) {
      if (removeFlags[item.chave]) {
        payload[`remover${item.chave}`] = true;
      } else if (form[item.chave]) {
        payload[item.chave] = form[item.chave];
      }
    } else {
      payload[item.chave] = form[item.chave];
    }
  });

  if (Object.keys(payload).some((key) => key.startsWith('remover'))) {
    const confirmed = await confirmAction({
      title: 'Remover segredo',
      message: 'A configuracao sensivel sera removida e o fallback sera usado, se existir.',
      confirmLabel: 'Remover'
    });
    if (!confirmed) return;
  }

  saving.value = true;
  error.value = '';
  try {
    configs[selected.value] = await salvarConfiguracaoCategoria(selected.value, payload);
    status.value = await obterStatusConfiguracoes();
    hydrate();
    showToast({ type: 'success', title: 'Configuracoes salvas' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel salvar as configuracoes.');
    showToast({ type: 'error', title: 'Erro ao salvar', message: error.value });
  } finally {
    saving.value = false;
  }
}

async function testar() {
  testing.value = true;
  error.value = '';
  try {
    const result = await testarConfiguracao(selected.value);
    showToast({ type: result.sucesso ? 'success' : 'error', title: result.status, message: result.urlExemplo || result.modelo });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel testar a configuracao.');
    showToast({ type: 'error', title: 'Erro no teste', message: error.value });
  } finally {
    testing.value = false;
  }
}

function labelCategoria(categoria: CategoriaConfiguracao) {
  const labels: Record<CategoriaConfiguracao, string> = {
    OpenRouter: 'OpenRouter',
    CampaignGeneration: 'Geracao',
    WhatsApp: 'WhatsApp',
    LeadCapture: 'Captura de leads',
    ExternalLeadApi: 'API externa',
    Application: 'Aplicacao',
    Landing: 'Landing',
    GoogleAds: 'Google Ads',
    MetaAds: 'Meta Ads'
  };
  return labels[categoria];
}

function description(categoria: CategoriaConfiguracao) {
  const labels: Record<CategoriaConfiguracao, string> = {
    OpenRouter: 'Modelo, chave e parametros da IA.',
    CampaignGeneration: 'Provider ativo e fallback controlado.',
    WhatsApp: 'Numero e mensagem usados na URL final.',
    LeadCapture: 'Consentimento, anti-spam e duplicidade.',
    ExternalLeadApi: 'Preparacao para API externa futura.',
    Application: 'URL publica para landings e links.',
    Landing: 'Textos padrao da experiencia publica.',
    GoogleAds: 'OAuth, developer token e conta padrao para publicacao futura.',
    MetaAds: 'App, segredo e callback para preparar OAuth futuro.'
  };
  return labels[categoria];
}

function message(err: unknown, fallback: string) {
  const response = err as { response?: { data?: { mensagem?: string } } };
  return response.response?.data?.mensagem || fallback;
}

function applyMetaList<T>(result: MetaAdsAssetListResponse<T>, target: { value: T[] }) {
  if (result.permissaoNecessaria) {
    metaPermissionNeeded.value = true;
  }
  if (result.mensagem) {
    metaAssetsMessage.value = result.mensagem;
  }
  target.value = result.itens;
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value));
}
</script>
