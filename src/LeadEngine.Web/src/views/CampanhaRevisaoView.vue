<template>
  <main class="page review-page">
    <section class="page-header row">
      <div>
        <p class="eyebrow">Revisao comercial</p>
        <h1>{{ form.nome || 'Campanha' }}</h1>
        <p class="subtitle">{{ localizacao }} · <span class="status">{{ campanha?.status || 'Carregando' }}</span></p>
      </div>
      <div class="actions header-actions">
        <RouterLink class="button secondary" to="/campanhas">Voltar</RouterLink>
        <button class="button secondary" :disabled="busy" @click="loadHistorico">Historico</button>
        <button class="button secondary" :disabled="busy || !campanha?.publicada" @click="copyPublicUrl">Copiar URL publica</button>
        <RouterLink v-if="campanha?.status === 'Revisada' && campanha?.publicada" class="button secondary" :to="`/campanhas/${campanha.id}/googleads-preview`">Preview Google Ads</RouterLink>
        <RouterLink v-if="campanha?.status === 'Revisada' && campanha?.publicada" class="button secondary" :to="`/campanhas/${campanha.id}/metaads-preview`">Preview Meta Ads</RouterLink>
        <button v-if="!campanha?.publicada" class="button secondary" :disabled="busy || !canPublicarLanding" @click="publicar">{{ publishing ? 'Publicando...' : 'Publicar landing' }}</button>
        <button v-else class="button secondary" :disabled="busy || !campanha" @click="despublicar">{{ publishing ? 'Despublicando...' : 'Despublicar' }}</button>
        <button class="button" :disabled="busy || !campanha" @click="aprovar">{{ approving ? 'Aprovando...' : 'Aprovar campanha' }}</button>
      </div>
    </section>

    <p v-if="error" class="error">{{ error }}</p>
    <p v-if="saved" class="success">Salvo.</p>

    <section v-if="loading" class="panel review-section">Carregando campanha...</section>
    <section v-else-if="campanha" class="review-grid">
      <ReviewBlock title="Informacoes gerais" secao="Nome" :dirty="dirty" :busy="busy" @save="save" @cancel="reset" @regenerate="startRegeneration('Nome')">
        <label>Nome<input v-model="form.nome" maxlength="180" /></label>
        <dl class="compact-list">
          <dt>Publico</dt><dd>{{ campanha.tipoPublico }}</dd>
          <dt>Operadora</dt><dd>{{ campanha.operadora }}</dd>
          <dt>Orcamento</dt><dd>{{ money(campanha.orcamentoDiario) }}</dd>
          <dt>Slug</dt><dd>{{ campanha.slug }}</dd>
          <dt>Publicacao</dt><dd>{{ campanha.publicada ? 'Publicada' : 'Despublicada' }}</dd>
          <dt>Data publicacao</dt><dd>{{ campanha.dataPublicacao ? dateTime(campanha.dataPublicacao) : '-' }}</dd>
          <dt>URL publica</dt><dd>{{ publicUrl }}</dd>
        </dl>
      </ReviewBlock>

      <ReviewBlock title="Landing page" secao="LandingPage" :dirty="dirty" :busy="busy" @save="save" @cancel="reset" @regenerate="startRegeneration('LandingPage')">
        <label>Titulo<input v-model="form.tituloLandingPage" maxlength="180" /></label>
        <label>Subtitulo<textarea v-model="form.subtituloLandingPage" maxlength="300" rows="3" /></label>
        <label>Texto do botao<input v-model="form.textoBotao" maxlength="80" /></label>
        <section class="landing-preview">
          <p class="eyebrow">Preview</p>
          <h2>{{ form.tituloLandingPage || 'Titulo da landing' }}</h2>
          <p>{{ form.subtituloLandingPage || 'Subtitulo da landing' }}</p>
          <span class="button preview-button">{{ form.textoBotao || 'CTA' }}</span>
        </section>
      </ReviewBlock>

      <ReviewBlock title="WhatsApp" secao="MensagemWhatsApp" :dirty="dirty" :busy="busy" @save="save" @cancel="reset" @regenerate="startRegeneration('MensagemWhatsApp')">
        <label>Mensagem<textarea v-model="form.mensagemWhatsApp" maxlength="500" rows="5" /></label>
      </ReviewBlock>

      <ReviewBlock title="Beneficios" secao="Beneficios" :dirty="dirty" :busy="busy" @save="save" @cancel="reset" @regenerate="startRegeneration('Beneficios')">
        <div v-for="(_, index) in form.beneficios" :key="`beneficio-${index}`" class="inline-edit">
          <input v-model="form.beneficios[index]" maxlength="120" />
          <button class="mini-button" :disabled="busy" @click="removeItem(form.beneficios, index)">Remover</button>
        </div>
        <button class="button secondary narrow" :disabled="busy || form.beneficios.length >= 6" @click="form.beneficios.push('')">Adicionar</button>
      </ReviewBlock>

      <ReviewBlock title="FAQ" secao="PerguntasFrequentes" :dirty="dirty" :busy="busy" @save="save" @cancel="reset" @regenerate="startRegeneration('PerguntasFrequentes')">
        <div v-for="(_, index) in form.perguntasFrequentes" :key="`faq-${index}`" class="faq-edit">
          <label>Pergunta<input v-model="form.perguntasFrequentes[index].pergunta" maxlength="180" /></label>
          <label>Resposta<textarea v-model="form.perguntasFrequentes[index].resposta" maxlength="500" rows="3" /></label>
          <button class="mini-button" :disabled="busy" @click="removeItem(form.perguntasFrequentes, index)">Remover</button>
        </div>
        <button class="button secondary narrow" :disabled="busy || form.perguntasFrequentes.length >= 6" @click="form.perguntasFrequentes.push({ pergunta: '', resposta: '' })">Adicionar</button>
      </ReviewBlock>

      <ReviewBlock title="Palavras-chave" secao="PalavrasChave" :dirty="dirty" :busy="busy" @save="save" @cancel="reset" @regenerate="startRegeneration('PalavrasChave')">
        <div v-for="(_, index) in form.palavrasChave" :key="`kw-${index}`" class="inline-edit">
          <input v-model="form.palavrasChave[index]" maxlength="120" />
          <button class="mini-button" :disabled="busy" @click="removeItem(form.palavrasChave, index)">Remover</button>
        </div>
        <button class="button secondary narrow" :disabled="busy" @click="form.palavrasChave.push('')">Adicionar</button>
      </ReviewBlock>

      <ReviewBlock title="Palavras negativas" secao="PalavrasChaveNegativas" :dirty="dirty" :busy="busy" @save="save" @cancel="reset" @regenerate="startRegeneration('PalavrasChaveNegativas')">
        <div v-for="(_, index) in form.palavrasChaveNegativas" :key="`neg-${index}`" class="inline-edit">
          <input v-model="form.palavrasChaveNegativas[index]" maxlength="120" />
          <button class="mini-button" :disabled="busy" @click="removeItem(form.palavrasChaveNegativas, index)">Remover</button>
        </div>
        <button class="button secondary narrow" :disabled="busy" @click="form.palavrasChaveNegativas.push('')">Adicionar</button>
      </ReviewBlock>

      <ReviewBlock title="Titulos dos anuncios" secao="TitulosAnuncios" :dirty="dirty" :busy="busy" @save="save" @cancel="reset" @regenerate="startRegeneration('TitulosAnuncios')">
        <div v-for="(_, index) in form.titulosAnuncios" :key="`title-${index}`" class="inline-edit">
          <input v-model="form.titulosAnuncios[index]" maxlength="30" />
          <span class="counter">{{ form.titulosAnuncios[index].length }}/30</span>
          <button class="mini-button" :disabled="busy" @click="removeItem(form.titulosAnuncios, index)">Remover</button>
        </div>
        <button class="button secondary narrow" :disabled="busy || form.titulosAnuncios.length >= 12" @click="form.titulosAnuncios.push('')">Adicionar</button>
      </ReviewBlock>

      <ReviewBlock title="Descricoes dos anuncios" secao="DescricoesAnuncios" :dirty="dirty" :busy="busy" @save="save" @cancel="reset" @regenerate="startRegeneration('DescricoesAnuncios')">
        <div v-for="(_, index) in form.descricoesAnuncios" :key="`desc-${index}`" class="inline-edit">
          <textarea v-model="form.descricoesAnuncios[index]" maxlength="90" rows="2" />
          <span class="counter">{{ form.descricoesAnuncios[index].length }}/90</span>
          <button class="mini-button" :disabled="busy" @click="removeItem(form.descricoesAnuncios, index)">Remover</button>
        </div>
        <button class="button secondary narrow" :disabled="busy || form.descricoesAnuncios.length >= 4" @click="form.descricoesAnuncios.push('')">Adicionar</button>
      </ReviewBlock>
    </section>

    <div v-if="regeneratingSection" class="modal-backdrop">
      <form class="panel modal" @submit.prevent="regenerate">
        <h2>Regenerar com IA</h2>
        <p>Secao: {{ regeneratingSection }}</p>
        <label>Instrucao adicional<textarea v-model="instrucaoAdicional" rows="4" placeholder="Deixe mais direto e focado em PME." /></label>
        <div class="actions">
          <button class="button secondary" type="button" :disabled="busy" @click="regeneratingSection = null">Cancelar</button>
          <button class="button" :disabled="busy">{{ regenerating ? 'Regenerando...' : 'Substituir secao' }}</button>
        </div>
      </form>
    </div>

    <aside v-if="showHistorico" class="history-panel panel">
      <div class="row between">
        <h2>Historico</h2>
        <button class="mini-button" @click="showHistorico = false">Fechar</button>
      </div>
      <p v-if="historico.length === 0">Nenhum registro.</p>
      <article v-for="item in historico" :key="`${item.data}-${item.resumoAlteracao}`" class="history-item">
        <strong>{{ item.resumoAlteracao }}</strong>
        <span>{{ dateTime(item.data) }} · {{ item.origem }}</span>
        <small v-if="item.provider">{{ item.provider }} / {{ item.modelo }}</small>
      </article>
    </aside>
  </main>
</template>

<script setup lang="ts">
import { computed, defineComponent, h, onMounted, reactive, ref } from 'vue';
import { useRoute } from 'vue-router';
import { confirmAction, showToast } from '../components/uiEvents';
import {
  aprovarCampanha,
  despublicarCampanha,
  listarHistoricoRevisoes,
  obterRevisaoCampanha,
  publicarCampanha,
  regenerarCampanhaSecao,
  revisarCampanha,
  type Campanha,
  type CampanhaSecao,
  type HistoricoRevisao,
  type RevisarCampanhaRequest
} from '../services/api';

const route = useRoute();
const campanha = ref<Campanha | null>(null);
const baseline = ref('');
const loading = ref(false);
const saving = ref(false);
const regenerating = ref(false);
const approving = ref(false);
const publishing = ref(false);
const error = ref('');
const saved = ref(false);
const regeneratingSection = ref<CampanhaSecao | null>(null);
const instrucaoAdicional = ref('');
const showHistorico = ref(false);
const historico = ref<HistoricoRevisao[]>([]);

const form = reactive<RevisarCampanhaRequest>({
  nome: '',
  tituloLandingPage: '',
  subtituloLandingPage: '',
  textoBotao: '',
  mensagemWhatsApp: '',
  beneficios: [],
  perguntasFrequentes: [],
  palavrasChave: [],
  palavrasChaveNegativas: [],
  titulosAnuncios: [],
  descricoesAnuncios: []
});

const busy = computed(() => loading.value || saving.value || regenerating.value || approving.value || publishing.value);
const dirty = computed(() => JSON.stringify(form) !== baseline.value);
const localizacao = computed(() => campanha.value ? [campanha.value.regiao, campanha.value.cidade, campanha.value.estado].filter(Boolean).join(' / ') : '');
const canPublicarLanding = computed(() => campanha.value?.status === 'Revisada');
const publicUrl = computed(() => {
  if (!campanha.value) return '';
  const base = `${window.location.origin}${import.meta.env.BASE_URL}`.replace(/\/+$/, '');
  return `${base}/lp/${campanha.value.slug}`;
});

const ReviewBlock = defineComponent({
  props: {
    title: { type: String, required: true },
    secao: { type: String, required: true },
    dirty: { type: Boolean, required: true },
    busy: { type: Boolean, required: true }
  },
  emits: ['save', 'cancel', 'regenerate'],
  setup(props, { emit, slots }) {
    return () => h('section', { class: 'panel review-section' }, [
      h('header', { class: 'review-section-header' }, [
        h('div', [
          h('h2', props.title),
          props.dirty ? h('span', { class: 'unsaved' }, 'alteracoes nao salvas') : null
        ]),
        h('div', { class: 'actions' }, [
          h('button', { class: 'button secondary', disabled: props.busy, onClick: () => emit('cancel') }, 'Cancelar'),
          h('button', { class: 'button secondary', disabled: props.busy, onClick: () => emit('regenerate') }, 'Regenerar IA'),
          h('button', { class: 'button', disabled: props.busy || !props.dirty, onClick: () => emit('save') }, 'Salvar')
        ])
      ]),
      h('div', { class: 'review-fields' }, slots.default?.())
    ]);
  }
});

onMounted(load);

async function load() {
  loading.value = true;
  error.value = '';
  try {
    campanha.value = await obterRevisaoCampanha(String(route.params.id));
    hydrate(campanha.value);
  } catch {
    error.value = 'Nao foi possivel carregar a campanha.';
  } finally {
    loading.value = false;
  }
}

async function save() {
  if (!campanha.value || saving.value) return;
  saving.value = true;
  error.value = '';
  saved.value = false;
  try {
    campanha.value = await revisarCampanha(campanha.value.id, payload());
    hydrate(campanha.value);
    saved.value = true;
    showToast({ type: 'success', title: 'Revisao salva', message: 'A campanha voltou para Gerada quando necessario.' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel salvar a revisao.');
    showToast({ type: 'error', title: 'Erro ao salvar', message: error.value });
  } finally {
    saving.value = false;
  }
}

function reset() {
  if (campanha.value) hydrate(campanha.value);
}

function startRegeneration(secao: CampanhaSecao) {
  regeneratingSection.value = secao;
  instrucaoAdicional.value = '';
}

async function regenerate() {
  if (!campanha.value || !regeneratingSection.value) return;
  const confirmed = await confirmAction({
    title: 'Substituir secao',
    message: 'O conteudo atual desta secao sera substituido pela resposta da IA.',
    confirmLabel: 'Substituir'
  });
  if (!confirmed) return;
  regenerating.value = true;
  error.value = '';
  try {
    campanha.value = await regenerarCampanhaSecao(campanha.value.id, regeneratingSection.value, instrucaoAdicional.value || undefined);
    hydrate(campanha.value);
    regeneratingSection.value = null;
    saved.value = true;
    showToast({ type: 'success', title: 'Secao regenerada', message: 'Revise o conteudo antes de publicar.' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel regenerar a secao.');
    showToast({ type: 'error', title: 'Erro na regeneracao', message: error.value });
  } finally {
    regenerating.value = false;
  }
}

async function aprovar() {
  if (!campanha.value) return;
  if (dirty.value) {
    const confirmed = await confirmAction({
      title: 'Aprovar campanha',
      message: 'Existem alteracoes nao salvas. A aprovacao usara a ultima versao salva.',
      confirmLabel: 'Aprovar'
    });
    if (!confirmed) return;
  }
  approving.value = true;
  error.value = '';
  try {
    campanha.value = await aprovarCampanha(campanha.value.id);
    hydrate(campanha.value);
    saved.value = true;
    showToast({ type: 'success', title: 'Campanha aprovada', message: 'Status alterado para Revisada.' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel aprovar a campanha.');
    showToast({ type: 'error', title: 'Erro ao aprovar', message: error.value });
  } finally {
    approving.value = false;
  }
}

async function publicar() {
  if (!campanha.value) return;
  if (dirty.value) {
    const confirmed = await confirmAction({
      title: 'Publicar landing',
      message: 'Existem alteracoes nao salvas. A publicacao usara a ultima versao salva.',
      confirmLabel: 'Publicar'
    });
    if (!confirmed) return;
  }
  publishing.value = true;
  error.value = '';
  try {
    await publicarCampanha(campanha.value.id);
    campanha.value = await obterRevisaoCampanha(campanha.value.id);
    hydrate(campanha.value);
    saved.value = true;
    showToast({ type: 'success', title: 'Landing publicada', message: publicUrl.value });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel publicar a landing.');
    showToast({ type: 'error', title: 'Erro ao publicar', message: error.value });
  } finally {
    publishing.value = false;
  }
}

async function despublicar() {
  if (!campanha.value) return;
  const confirmed = await confirmAction({
    title: 'Despublicar landing',
    message: 'A URL publica deixara de responder para novos visitantes.',
    confirmLabel: 'Despublicar'
  });
  if (!confirmed) return;
  publishing.value = true;
  error.value = '';
  try {
    await despublicarCampanha(campanha.value.id);
    campanha.value = await obterRevisaoCampanha(campanha.value.id);
    hydrate(campanha.value);
    saved.value = true;
    showToast({ type: 'info', title: 'Landing despublicada' });
  } catch (err: unknown) {
    error.value = message(err, 'Nao foi possivel despublicar a landing.');
    showToast({ type: 'error', title: 'Erro ao despublicar', message: error.value });
  } finally {
    publishing.value = false;
  }
}

async function copyPublicUrl() {
  await navigator.clipboard.writeText(publicUrl.value);
  saved.value = true;
  showToast({ type: 'success', title: 'URL copiada', message: publicUrl.value });
}

async function loadHistorico() {
  if (!campanha.value) return;
  historico.value = await listarHistoricoRevisoes(campanha.value.id);
  showHistorico.value = true;
}

function hydrate(source: Campanha) {
  form.nome = source.nome;
  form.tituloLandingPage = source.tituloLandingPage;
  form.subtituloLandingPage = source.subtituloLandingPage;
  form.textoBotao = source.textoBotao;
  form.mensagemWhatsApp = source.mensagemWhatsApp;
  form.beneficios = [...source.beneficios];
  form.perguntasFrequentes = source.perguntasFrequentes.map((item) => ({ ...item }));
  form.palavrasChave = [...source.palavrasChave];
  form.palavrasChaveNegativas = [...source.palavrasChaveNegativas];
  form.titulosAnuncios = [...source.titulosAnuncios];
  form.descricoesAnuncios = [...source.descricoesAnuncios];
  baseline.value = JSON.stringify(form);
}

function payload(): RevisarCampanhaRequest {
  return JSON.parse(JSON.stringify(form));
}

function removeItem<T>(items: T[], index: number) {
  items.splice(index, 1);
}

function money(value: number) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);
}

function dateTime(value: string) {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(value));
}

function message(err: unknown, fallback: string) {
  const response = err as { response?: { data?: { mensagem?: string } } };
  return response.response?.data?.mensagem || fallback;
}
</script>
