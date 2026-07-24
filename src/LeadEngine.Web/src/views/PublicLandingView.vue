<template>
  <main class="public-page">
    <section v-if="loading" class="public-band">Carregando...</section>
    <section v-else-if="error" class="public-band">
      <h1>Landing page indisponivel</h1>
      <p>{{ error }}</p>
    </section>
    <template v-else-if="campanha">
      <section class="public-hero">
        <p class="eyebrow">{{ campanha.nome }}</p>
        <h1>{{ campanha.titulo }}</h1>
        <p class="subtitle">{{ campanha.subtitulo }}</p>
      </section>

      <section class="public-layout">
        <div class="public-content">
          <section>
            <h2>Beneficios</h2>
            <ul class="benefit-list">
              <li v-for="beneficio in campanha.beneficios" :key="beneficio">{{ beneficio }}</li>
            </ul>
          </section>

          <section>
            <h2>FAQ</h2>
            <details v-for="item in campanha.perguntasFrequentes" :key="item.pergunta">
              <summary>{{ item.pergunta }}</summary>
              <p>{{ item.resposta }}</p>
            </details>
          </section>

          <p class="notice">Valores, redes, carencias e coberturas dependem do plano, perfil, regiao e regras da operadora.</p>
        </div>

        <form class="panel public-form" @submit.prevent="submit">
          <h2>Receber cotacao</h2>
          <label>Nome<input v-model.trim="form.nome" required maxlength="120" /></label>
          <label>Telefone<input v-model.trim="form.telefone" required maxlength="20" inputmode="tel" /></label>
          <label>E-mail<input v-model.trim="form.email" maxlength="160" type="email" /></label>
          <label>Cidade<input v-model.trim="form.cidade" required maxlength="100" /></label>
          <label>Estado<input v-model.trim="form.estado" required maxlength="2" /></label>
          <label>Quantidade de vidas<input v-model.number="form.quantidadeVidas" required type="number" min="1" max="999" /></label>
          <label>
            Tipo de contratacao
            <select v-model="form.tipoContratacao" required>
              <option value="Individual">Individual</option>
              <option value="Familiar">Familiar</option>
              <option value="Empresarial">Empresarial</option>
              <option value="Mei">MEI</option>
              <option value="AindaNaoSei">Ainda nao sei</option>
            </select>
          </label>
          <label class="wide">Observacao<textarea v-model.trim="form.observacao" maxlength="1000" rows="4" /></label>
          <label class="consent"><input v-model="form.consentimento" type="checkbox" required /> Autorizo o contato para receber informacoes e cotacoes de planos de saude.</label>
          <input v-model="form.website" class="hp-field" tabindex="-1" autocomplete="off" />
          <p v-if="submitError" class="error">{{ submitError }}</p>
          <p v-if="success" class="success">{{ success }}</p>
          <button class="button" :disabled="submitting">{{ submitting ? 'Enviando...' : campanha.textoBotao }}</button>
          <a v-if="whatsAppUrl" class="button secondary" :href="whatsAppUrl" target="_blank" rel="noopener">Abrir WhatsApp</a>
          <small>Usamos seus dados apenas para contato sobre esta solicitacao.</small>
        </form>
      </section>
    </template>
  </main>
</template>

<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue';
import { useRoute } from 'vue-router';
import { capturarLeadPublico, obterCampanhaPublica, type CampanhaPublica, type CapturarLeadPublicoRequest } from '../services/api';

const route = useRoute();
const campanha = ref<CampanhaPublica | null>(null);
const loading = ref(false);
const submitting = ref(false);
const error = ref('');
const submitError = ref('');
const success = ref('');
const whatsAppUrl = ref('');
const openedAt = Date.now();

const form = reactive<CapturarLeadPublicoRequest>({
  nome: '',
  telefone: '',
  email: '',
  cidade: '',
  estado: '',
  quantidadeVidas: 1,
  tipoContratacao: 'Familiar',
  observacao: '',
  consentimento: false,
  website: '',
  formOpenedAt: openedAt
});

onMounted(async () => {
  loading.value = true;
  try {
    campanha.value = await obterCampanhaPublica(String(route.params.slug));
    form.cidade = campanha.value.cidade;
    form.estado = campanha.value.estado;
    applyTracking();
  } catch {
    error.value = 'A campanha nao esta ativa ou nao existe.';
  } finally {
    loading.value = false;
  }
});

async function submit() {
  if (submitting.value) return;
  submitting.value = true;
  submitError.value = '';
  success.value = '';
  try {
    const response = await capturarLeadPublico(String(route.params.slug), { ...form, estado: form.estado.toUpperCase(), formOpenedAt: openedAt });
    success.value = response.mensagem;
    whatsAppUrl.value = response.whatsAppUrl;
    window.open(response.whatsAppUrl, '_blank', 'noopener');
  } catch (err: unknown) {
    const response = err as { response?: { data?: { mensagem?: string } } };
    submitError.value = response.response?.data?.mensagem || 'Nao foi possivel enviar seus dados. Tente novamente.';
  } finally {
    submitting.value = false;
  }
}

function applyTracking() {
  const params = new URLSearchParams(window.location.search);
  form.utmSource = params.get('utm_source') || undefined;
  form.utmMedium = params.get('utm_medium') || undefined;
  form.utmCampaign = params.get('utm_campaign') || undefined;
  form.utmTerm = params.get('utm_term') || undefined;
  form.utmContent = params.get('utm_content') || undefined;
  form.gclid = params.get('gclid') || undefined;
  form.fbclid = params.get('fbclid') || undefined;
}
</script>
