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
                  {{ item.configurado ? 'Chave configurada' : 'Nao configurada' }}
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
        </template>
      </section>
    </section>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue';
import MetricCard from '../components/MetricCard.vue';
import SkeletonBlock from '../components/SkeletonBlock.vue';
import { confirmAction, showToast } from '../components/uiEvents';
import {
  obterConfiguracaoCategoria,
  obterStatusConfiguracoes,
  salvarConfiguracaoCategoria,
  testarConfiguracao,
  type CategoriaConfiguracao,
  type ConfiguracaoCategoria,
  type ConfiguracoesStatus
} from '../services/api';

const categorias: CategoriaConfiguracao[] = ['OpenRouter', 'CampaignGeneration', 'WhatsApp', 'LeadCapture', 'ExternalLeadApi', 'Application', 'Landing'];
const selected = ref<CategoriaConfiguracao>('OpenRouter');
const configs = reactive<Partial<Record<CategoriaConfiguracao, ConfiguracaoCategoria>>>({});
const status = ref<ConfiguracoesStatus | null>(null);
const loading = ref(false);
const saving = ref(false);
const testing = ref(false);
const error = ref('');
const form = reactive<Record<string, string>>({});
const removeFlags = reactive<Record<string, boolean>>({});

const current = computed(() => configs[selected.value]);

onMounted(load);
watch(selected, hydrate);

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
  } catch {
    error.value = 'Nao foi possivel carregar as configuracoes.';
  } finally {
    loading.value = false;
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
    Landing: 'Landing'
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
    Landing: 'Textos padrao da experiencia publica.'
  };
  return labels[categoria];
}

function message(err: unknown, fallback: string) {
  const response = err as { response?: { data?: { mensagem?: string } } };
  return response.response?.data?.mensagem || fallback;
}
</script>
