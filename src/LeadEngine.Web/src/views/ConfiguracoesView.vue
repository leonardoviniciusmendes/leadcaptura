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
    </section>

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
                  <span>{{ conta.customerId }} <template v-if="conta.email">Â· {{ conta.email }}</template></span>
                </div>
                <button class="button secondary" :disabled="googleBusy || conta.padrao" @click="selecionarConta(conta.id)">
                  {{ conta.padrao ? 'Conta padrao' : 'Selecionar' }}
                </button>
              </article>
            </div>
            <EmptyState v-else title="Nenhuma conta conectada" message="Conecte uma conta Google para preparar a publicacao futura no Google Ads." />
          </section>
        </template>
      </section>
    </section>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import MetricCard from '../components/MetricCard.vue';
import SkeletonBlock from '../components/SkeletonBlock.vue';
import EmptyState from '../components/EmptyState.vue';
import { confirmAction, showToast } from '../components/uiEvents';
import {
  concluirGoogleAdsOAuth,
  listarGoogleAdsContas,
  obterGoogleAdsAuthUrl,
  obterGoogleAdsStatus,
  obterConfiguracaoCategoria,
  obterStatusConfiguracoes,
  selecionarGoogleAdsConta,
  salvarConfiguracaoCategoria,
  testarGoogleAds,
  testarConfiguracao,
  type CategoriaConfiguracao,
  type ConfiguracaoCategoria,
  type ConfiguracoesStatus,
  type GoogleAdsConta,
  type GoogleAdsStatus
} from '../services/api';

const categorias: CategoriaConfiguracao[] = ['OpenRouter', 'CampaignGeneration', 'WhatsApp', 'LeadCapture', 'ExternalLeadApi', 'Application', 'Landing', 'GoogleAds'];
const selected = ref<CategoriaConfiguracao>('OpenRouter');
const configs = reactive<Partial<Record<CategoriaConfiguracao, ConfiguracaoCategoria>>>({});
const status = ref<ConfiguracoesStatus | null>(null);
const googleStatus = ref<GoogleAdsStatus | null>(null);
const googleContas = ref<GoogleAdsConta[]>([]);
const loading = ref(false);
const saving = ref(false);
const testing = ref(false);
const googleBusy = ref(false);
const error = ref('');
const form = reactive<Record<string, string>>({});
const removeFlags = reactive<Record<string, boolean>>({});

const current = computed(() => configs[selected.value]);

onMounted(async () => {
  await handleGoogleCallback();
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
  } catch {
    error.value = `Nao foi possivel carregar as configuracoes de ${labelCategoria(selected.value)}.`;
  } finally {
    loading.value = false;
  }
}

async function loadGoogleAds() {
  try {
    const [statusResult, contasResult] = await Promise.all([obterGoogleAdsStatus(), listarGoogleAdsContas()]);
    googleStatus.value = statusResult;
    googleContas.value = contasResult;
  } catch {
    googleStatus.value = { conectado: false, status: 'Erro' };
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

async function handleGoogleCallback() {
  const params = new URLSearchParams(window.location.search);
  const code = params.get('code');
  const googleCallback = params.get('googleAdsCallback');
  if (!code || !googleCallback) return;

  const state = params.get('state') || undefined;
  const expectedState = window.sessionStorage.getItem('googleAdsOAuthState');
  if (expectedState && state && expectedState !== state) {
    showToast({ type: 'error', title: 'OAuth invalido', message: 'State recebido nao confere.' });
    return;
  }

  googleBusy.value = true;
  selected.value = 'GoogleAds';
  try {
    googleContas.value = await concluirGoogleAdsOAuth({
      code,
      state,
      redirectUri: `${window.location.origin}${window.location.pathname}?googleAdsCallback=1`
    });
    googleStatus.value = await obterGoogleAdsStatus();
    window.history.replaceState({}, document.title, `${window.location.origin}${window.location.pathname}`);
    showToast({ type: 'success', title: 'Google Ads conectado' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel concluir a conexao Google Ads.');
    showToast({ type: 'error', title: 'Erro no OAuth', message: error.value });
  } finally {
    googleBusy.value = false;
    window.sessionStorage.removeItem('googleAdsOAuthState');
  }
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
    GoogleAds: 'Google Ads'
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
    GoogleAds: 'OAuth, developer token e conta padrao para publicacao futura.'
  };
  return labels[categoria];
}

function message(err: unknown, fallback: string) {
  const response = err as { response?: { data?: { mensagem?: string } } };
  return response.response?.data?.mensagem || fallback;
}
</script>
