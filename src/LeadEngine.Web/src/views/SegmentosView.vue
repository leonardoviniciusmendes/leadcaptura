<template>
  <main class="page">
    <section class="page-header row">
      <div>
        <p class="eyebrow">Configuracoes</p>
        <h1>Segmentos</h1>
        <p class="subtitle">Cadastre nichos de campanha sem alterar codigo ou publicar uma nova versao.</p>
      </div>
      <button class="button" @click="novo">Novo segmento</button>
    </section>

    <p v-if="error" class="error">{{ error }}</p>
    <p v-if="success" class="status-line">{{ success }}</p>

    <section class="settings-layout">
      <section class="panel settings-panel">
        <SkeletonBlock v-if="loading" :count="6" />
        <template v-else>
          <header class="section-heading">
            <div>
              <h2>Segmentos cadastrados</h2>
              <span>{{ segments.length }} registros</span>
            </div>
          </header>

          <table v-if="segments.length" class="admin-table">
            <thead>
              <tr>
                <th>Nome</th>
                <th>Slug</th>
                <th>Template</th>
                <th>Status</th>
                <th>Campanhas</th>
                <th>Acoes</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="segment in segments" :key="segment.id">
                <td>{{ segment.name }}</td>
                <td><code>{{ segment.slug }}</code></td>
                <td><code>{{ segment.templateKey }}</code></td>
                <td>
                  <span class="status" :class="segment.isActive ? 'status-revisada' : 'status-pausada'">
                    {{ segment.isActive ? 'Ativo' : 'Inativo' }}
                  </span>
                </td>
                <td>{{ segment.campaignsCount }}</td>
                <td>
                  <div class="actions inline">
                    <button class="button secondary" @click="editar(segment)">Editar</button>
                    <button class="button secondary" :disabled="saving" @click="alternarStatus(segment)">
                      {{ segment.isActive ? 'Desativar' : 'Ativar' }}
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
          <EmptyState v-else title="Nenhum segmento" message="Crie o primeiro segmento para disponibiliza-lo na tela de nova campanha." />
        </template>
      </section>

      <aside v-if="editing" class="panel settings-panel segment-form-panel">
        <header class="section-heading">
          <div>
            <h2>{{ form.id ? 'Editar segmento' : 'Novo segmento' }}</h2>
            <span>O JSON e apenas configuracao declarativa.</span>
          </div>
        </header>

        <form class="settings-form" @submit.prevent="salvar">
          <label>
            Nome *
            <input v-model.trim="form.name" required />
          </label>

          <label>
            Slug *
            <input v-model.trim="form.slug" required placeholder="ex: estetica" />
          </label>

          <label>
            Descricao
            <textarea v-model.trim="form.description" rows="3" />
          </label>

          <label>
            Template *
            <input v-model.trim="form.templateKey" required placeholder="ex: local_service_lead_generation" />
          </label>

          <label class="checkbox-line">
            <input v-model="form.isActive" type="checkbox" />
            Ativo
          </label>

          <label>
            Configuracao avancada: DefaultConfigJson
            <textarea v-model="form.defaultConfigJson" rows="10" spellcheck="false" placeholder="{ }" />
          </label>

          <div class="actions">
            <button class="button secondary" type="button" :disabled="saving" @click="cancelar">Cancelar</button>
            <button class="button" :disabled="saving">{{ saving ? 'Salvando...' : 'Salvar' }}</button>
          </div>
        </form>
      </aside>
    </section>
  </main>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import EmptyState from '../components/EmptyState.vue';
import SkeletonBlock from '../components/SkeletonBlock.vue';
import { showToast } from '../components/uiEvents';
import {
  atualizarAdminSegment,
  atualizarAdminSegmentStatus,
  criarAdminSegment,
  listarAdminSegments,
  type AdminSegment,
  type UpsertSegmentRequest
} from '../services/api';

const segments = ref<AdminSegment[]>([]);
const loading = ref(false);
const saving = ref(false);
const editing = ref(false);
const error = ref('');
const success = ref('');
const form = reactive({
  id: '',
  name: '',
  slug: '',
  description: '',
  templateKey: '',
  defaultConfigJson: '',
  isActive: true
});

onMounted(load);

async function load() {
  loading.value = true;
  error.value = '';
  try {
    segments.value = await listarAdminSegments();
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel carregar os segmentos.');
  } finally {
    loading.value = false;
  }
}

function novo() {
  hydrate();
  editing.value = true;
}

function editar(segment: AdminSegment) {
  hydrate(segment);
  editing.value = true;
}

function cancelar() {
  editing.value = false;
  hydrate();
}

async function salvar() {
  error.value = '';
  success.value = '';
  if (!validar()) return;

  const payload: UpsertSegmentRequest = {
    name: form.name,
    slug: form.slug,
    description: form.description || undefined,
    templateKey: form.templateKey,
    defaultConfigJson: form.defaultConfigJson.trim() || undefined,
    isActive: form.isActive
  };

  saving.value = true;
  try {
    if (form.id) {
      await atualizarAdminSegment(form.id, payload);
      success.value = 'Segmento atualizado.';
    } else {
      await criarAdminSegment(payload);
      success.value = 'Segmento criado.';
    }
    showToast({ type: 'success', title: success.value });
    editing.value = false;
    hydrate();
    await load();
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel salvar o segmento.');
    showToast({ type: 'error', title: 'Erro ao salvar', message: error.value });
  } finally {
    saving.value = false;
  }
}

async function alternarStatus(segment: AdminSegment) {
  saving.value = true;
  error.value = '';
  success.value = '';
  try {
    await atualizarAdminSegmentStatus(segment.id, !segment.isActive);
    success.value = segment.isActive ? 'Segmento desativado.' : 'Segmento ativado.';
    showToast({ type: 'success', title: success.value });
    await load();
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel alterar o status.');
  } finally {
    saving.value = false;
  }
}

function validar() {
  if (!form.name.trim()) {
    error.value = 'Nome do segmento e obrigatorio.';
    return false;
  }
  if (!form.slug.trim()) {
    error.value = 'Slug do segmento e obrigatorio.';
    return false;
  }
  if (!form.templateKey.trim()) {
    error.value = 'Template do segmento e obrigatorio.';
    return false;
  }
  if (form.defaultConfigJson.trim()) {
    try {
      JSON.parse(form.defaultConfigJson);
    } catch {
      error.value = 'DefaultConfigJson deve ser um JSON valido.';
      return false;
    }
  }
  return true;
}

function hydrate(segment?: AdminSegment) {
  form.id = segment?.id || '';
  form.name = segment?.name || '';
  form.slug = segment?.slug || '';
  form.description = segment?.description || '';
  form.templateKey = segment?.templateKey || '';
  form.defaultConfigJson = segment?.defaultConfigJson || '';
  form.isActive = segment?.isActive ?? true;
}

function message(err: unknown, fallback: string) {
  const response = err as { response?: { data?: { mensagem?: string; title?: string; errors?: Record<string, string[]> } } };
  const errors = response.response?.data?.errors;
  const firstError = errors ? Object.values(errors).flat()[0] : undefined;
  return response.response?.data?.mensagem || firstError || response.response?.data?.title || fallback;
}
</script>
